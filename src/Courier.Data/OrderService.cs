using System.Data;
using Courier.Shared;
using Microsoft.Data.SqlClient;

namespace Courier.Data;

public static class OrderService
{
    private const string View = "dbo.VW_KS_Zakazy";

    public static DataTable GetOrders(string? search = null, int? statusId = null,
        DateTime? from = null, DateTime? to = null, int? courierId = null)
    {
        var sql = $"""
            SELECT id_zakaza, nomer, id_klienta, klient, telefon_klienta,
                   id_zony, zona, ves, cena,
                   status, id_statusa, kurier, id_kuriera, data_sozdaniya, data_dostavki,
                   adres_otpravitelya, fio_poluchatelya, telefon_poluchatelya,
                   adres_poluchatelya, kommentariy, sposob_oplaty
            FROM {View}
            WHERE (@search = N'' OR nomer LIKE @like OR klient LIKE @like
                   OR telefon_klienta LIKE @like OR fio_poluchatelya LIKE @like)
              AND (@status IS NULL OR id_statusa = @status)
              AND (@from IS NULL OR data_sozdaniya >= @from)
              AND (@to IS NULL OR data_sozdaniya < DATEADD(DAY, 1, @to))
              AND (@courier IS NULL OR id_kuriera = @courier)
            ORDER BY data_sozdaniya DESC, id_zakaza DESC;
            """;

        var text = (search ?? "").Trim();
        return Db.Query(sql,
            new SqlParameter("@search", text),
            new SqlParameter("@like", "%" + text + "%"),
            new SqlParameter("@status", (object?)statusId ?? DBNull.Value),
            new SqlParameter("@from", (object?)from ?? DBNull.Value),
            new SqlParameter("@to", (object?)to ?? DBNull.Value),
            new SqlParameter("@courier", (object?)courierId ?? DBNull.Value));
    }

    public static DataTable GetOrder(int id)
        => Db.Query($"SELECT * FROM {View} WHERE id_zakaza=@id", new SqlParameter("@id", id));

    public static OrderDto? GetOrderDto(int id)
    {
        var table = GetOrder(id);
        return table.Rows.Count == 0 ? null : MapOrder(table.Rows[0]);
    }

    public static DataTable GetHistory(int orderId)
        => Db.Query(
            """
            SELECT i.id_zapisi, i.id_statusa, s.nazvanie AS status,
                   ISNULL(p.fio, N'Система') AS polzovatel,
                   i.data_izmeneniya, i.kommentariy
            FROM dbo.KURS_KS_Istoriya_Statusov i
            JOIN dbo.KURS_KS_Status_Zakaza s ON s.id_statusa = i.id_statusa
            LEFT JOIN dbo.KURS_KS_Polzovatel p ON p.id_polzovatelya = i.id_polzovatelya
            WHERE i.id_zakaza = @id
            ORDER BY i.data_izmeneniya, i.id_zapisi;
            """,
            new SqlParameter("@id", orderId));

    public static List<HistoryDto> GetHistoryDto(int orderId)
        => GetHistory(orderId).Rows.Cast<DataRow>().Select(r => new HistoryDto
        {
            Id = Convert.ToInt32(r["id_zapisi"]),
            StatusId = Convert.ToInt32(r["id_statusa"]),
            Status = r["status"]?.ToString() ?? "",
            User = r["polzovatel"]?.ToString(),
            ChangedAt = Convert.ToDateTime(r["data_izmeneniya"]),
            Comment = r["kommentariy"]?.ToString()
        }).ToList();

    public static TaxResult GetTariff(int zoneId, decimal weight)
    {
        var table = Db.Query(
            """
            SELECT TOP (1) id_tarifa, cena
            FROM dbo.KURS_KS_Tarif
            WHERE id_zony=@zone AND @weight BETWEEN ves_ot AND ves_do
            ORDER BY ves_ot;
            """,
            new SqlParameter("@zone", zoneId),
            new SqlParameter("@weight", weight));

        if (table.Rows.Count == 0)
            throw new InvalidOperationException("Для выбранной зоны и веса не найден тариф.");

        return new TaxResult(
            Convert.ToInt32(table.Rows[0]["id_tarifa"]),
            Convert.ToDecimal(table.Rows[0]["cena"]));
    }

    public sealed record TaxResult(int TariffId, decimal Price);

    public static OrderCreatedResponse Create(CreateOrderRequest request, int? userId)
    {
        using var connection = Db.Open();
        using var command = new SqlCommand("dbo.sp_KS_Sozdat_Zakaz", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@id_klienta", request.ClientId);
        command.Parameters.AddWithValue("@adres_otpravitelya", request.SenderAddress);
        command.Parameters.AddWithValue("@fio_poluchatelya", request.RecipientFio);
        command.Parameters.AddWithValue("@telefon_poluchatelya", request.RecipientPhone);
        command.Parameters.AddWithValue("@adres_poluchatelya", request.RecipientAddress);
        command.Parameters.AddWithValue("@ves", request.Weight);
        command.Parameters.AddWithValue("@id_zony", request.ZoneId);
        command.Parameters.AddWithValue("@kommentariy", (object?)request.Comment ?? DBNull.Value);
        command.Parameters.AddWithValue("@sposob_oplaty", (object?)request.PaymentMethod ?? "Наличные");
        command.Parameters.AddWithValue("@id_polzovatelya", (object?)userId ?? DBNull.Value);
        var outId = new SqlParameter("@id_zakaza", SqlDbType.Int) { Direction = ParameterDirection.Output };
        command.Parameters.Add(outId);

        var result = new OrderCreatedResponse();
        using (var reader = command.ExecuteReader())
        {
            if (reader.Read())
            {
                result.Id = Convert.ToInt32(reader["id_zakaza"]);
                result.Nomer = reader["nomer"]?.ToString() ?? "";
                result.TariffId = reader["id_tarifa"] is DBNull ? null : Convert.ToInt32(reader["id_tarifa"]);
                result.Price = Convert.ToDecimal(reader["cena"]);
            }
        }

        if (result.Id == 0 && outId.Value is not DBNull and not null)
            result.Id = Convert.ToInt32(outId.Value);

        if (request.CourierId is > 0)
            AssignCourier(result.Id, request.CourierId.Value, userId);

        return result;
    }

    public static void Update(int orderId, UpdateOrderRequest request, int? userId)
    {
        var current = GetOrderDto(orderId)
            ?? throw new InvalidOperationException("Заказ не найден.");

        if (current.Status is "Доставлен" or "Отменён")
            throw new InvalidOperationException("Нельзя изменять завершённый или отменённый заказ.");

        var tariff = GetTariff(request.ZoneId, request.Weight);
        var oldCourier = current.CourierId;
        var newCourier = request.CourierId is > 0 ? request.CourierId : null;

        using var connection = Db.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            using (var command = new SqlCommand(
                """
                UPDATE dbo.KURS_KS_Zakaz
                SET id_klienta=@client,
                    adres_otpravitelya=@sender,
                    fio_poluchatelya=@rfio,
                    telefon_poluchatelya=@rphone,
                    adres_poluchatelya=@raddr,
                    ves=@weight,
                    id_zony=@zone,
                    id_tarifa=@tariff,
                    cena=@price,
                    kommentariy=@comment,
                    sposob_oplaty=@pay,
                    data_izmeneniya=SYSDATETIME()
                WHERE id_zakaza=@id;
                """, connection, transaction))
            {
                command.Parameters.AddWithValue("@client", request.ClientId);
                command.Parameters.AddWithValue("@sender", request.SenderAddress);
                command.Parameters.AddWithValue("@rfio", request.RecipientFio);
                command.Parameters.AddWithValue("@rphone", request.RecipientPhone);
                command.Parameters.AddWithValue("@raddr", request.RecipientAddress);
                command.Parameters.AddWithValue("@weight", request.Weight);
                command.Parameters.AddWithValue("@zone", request.ZoneId);
                command.Parameters.AddWithValue("@tariff", tariff.TariffId);
                command.Parameters.AddWithValue("@price", tariff.Price);
                command.Parameters.AddWithValue("@comment", (object?)request.Comment ?? DBNull.Value);
                command.Parameters.AddWithValue("@pay", (object?)request.PaymentMethod ?? DBNull.Value);
                command.Parameters.AddWithValue("@id", orderId);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        if (newCourier != oldCourier)
        {
            if (newCourier is > 0)
            {
                AssignCourier(orderId, newCourier.Value, userId);
            }
            else
            {
                UnassignCourier(orderId, userId);
            }
        }

        AuditService.Log(GetLogin(userId), $"Изменён заказ №{current.Nomer}", "KURS_KS_Zakaz", orderId);
    }

    public static void AssignCourier(int orderId, int courierId, int? userId)
        => ExecuteProc("dbo.sp_KS_Naznachit_Kuriera",
            new SqlParameter("@id_zakaza", orderId),
            new SqlParameter("@id_kuriera", courierId),
            new SqlParameter("@id_polzovatelya", (object?)userId ?? DBNull.Value));

    public static void ChangeStatus(int orderId, int statusId, int? userId, string? comment)
        => ExecuteProc("dbo.sp_KS_Izmenit_Status",
            new SqlParameter("@id_zakaza", orderId),
            new SqlParameter("@id_statusa", statusId),
            new SqlParameter("@id_polzovatelya", (object?)userId ?? DBNull.Value),
            new SqlParameter("@kommentariy", (object?)comment ?? DBNull.Value));

    public static void Cancel(int orderId, int? userId, string? comment = "Заказ отменён")
    {
        var statusId = Db.Scalar<int>(
            "SELECT id_statusa FROM dbo.KURS_KS_Status_Zakaza WHERE nazvanie=N'Отменён'");
        ChangeStatus(orderId, statusId, userId, comment);
    }

    public static void UnassignCourier(int orderId, int? userId)
    {
        var order = GetOrderDto(orderId);
        if (order?.CourierId is not int courierId) return;

        using var connection = Db.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            using (var command = new SqlCommand(
                """
                UPDATE dbo.KURS_KS_Zakaz
                SET id_kuriera=NULL,
                    id_statusa=(SELECT id_statusa FROM dbo.KURS_KS_Status_Zakaza WHERE nazvanie=N'Принят'),
                    data_izmeneniya=SYSDATETIME()
                WHERE id_zakaza=@id;

                UPDATE dbo.KURS_KS_Kurier
                SET status=CASE WHEN EXISTS
                (
                    SELECT 1 FROM dbo.KURS_KS_Zakaz z
                    JOIN dbo.KURS_KS_Status_Zakaza s ON s.id_statusa=z.id_statusa
                    WHERE z.id_kuriera=@courier AND s.nazvanie IN (N'Назначен', N'В пути')
                ) THEN N'Занят' ELSE N'Свободен' END
                WHERE id_kuriera=@courier;
                """, connection, transaction))
            {
                command.Parameters.AddWithValue("@id", orderId);
                command.Parameters.AddWithValue("@courier", courierId);
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        AuditService.Log(GetLogin(userId), $"Снят курьер с заказа №{order.Nomer}", "KURS_KS_Zakaz", orderId);
    }

    public static DashboardDto GetDashboard()
    {
        var result = new DashboardDto
        {
            OrdersToday = Db.Scalar<int>(
                "SELECT COUNT(*) FROM dbo.KURS_KS_Zakaz WHERE CAST(data_sozdaniya AS date)=CAST(SYSDATETIME() AS date)"),
            OrdersInWork = Db.Scalar<int>(
                """
                SELECT COUNT(*) FROM dbo.KURS_KS_Zakaz z
                JOIN dbo.KURS_KS_Status_Zakaza s ON s.id_statusa=z.id_statusa
                WHERE s.nazvanie IN (N'Принят', N'Назначен', N'В пути')
                """),
            DeliveredThisMonth = Db.Scalar<int>(
                """
                SELECT COUNT(*) FROM dbo.KURS_KS_Zakaz z
                JOIN dbo.KURS_KS_Status_Zakaza s ON s.id_statusa=z.id_statusa
                WHERE s.nazvanie=N'Доставлен'
                  AND z.data_dostavki >= DATEFROMPARTS(YEAR(SYSDATETIME()), MONTH(SYSDATETIME()), 1)
                """),
            FreeCouriers = Db.Scalar<int>(
                "SELECT COUNT(*) FROM dbo.KURS_KS_Kurier WHERE is_active=1 AND status=N'Свободен'"),
            RevenueThisMonth = Db.Scalar<decimal>(
                """
                SELECT ISNULL(SUM(z.cena), 0) FROM dbo.KURS_KS_Zakaz z
                JOIN dbo.KURS_KS_Status_Zakaza s ON s.id_statusa=z.id_statusa
                WHERE s.nazvanie<>N'Отменён'
                  AND z.data_sozdaniya >= DATEFROMPARTS(YEAR(SYSDATETIME()), MONTH(SYSDATETIME()), 1)
                """),
            ClientsTotal = Db.Scalar<int>("SELECT COUNT(*) FROM dbo.KURS_KS_Klient WHERE is_active=1")
        };

        var recent = Db.Query(
            $"SELECT TOP (8) * FROM {View} ORDER BY data_sozdaniya DESC, id_zakaza DESC");
        result.RecentOrders = recent.Rows.Cast<DataRow>().Select(MapOrder).ToList();
        return result;
    }

    public static OrderDto MapOrder(DataRow row) => new()
    {
        Id = Convert.ToInt32(row["id_zakaza"]),
        Nomer = row["nomer"]?.ToString() ?? "",
        ClientId = row.Table.Columns.Contains("id_klienta") ? Convert.ToInt32(row["id_klienta"]) : 0,
        Client = row["klient"]?.ToString() ?? "",
        ClientPhone = row["telefon_klienta"]?.ToString() ?? "",
        SenderAddress = row["adres_otpravitelya"]?.ToString() ?? "",
        RecipientFio = row["fio_poluchatelya"]?.ToString() ?? "",
        RecipientPhone = row["telefon_poluchatelya"]?.ToString() ?? "",
        RecipientAddress = row["adres_poluchatelya"]?.ToString() ?? "",
        Weight = Convert.ToDecimal(row["ves"]),
        ZoneId = Convert.ToInt32(row["id_zony"]),
        Zone = row["zona"]?.ToString() ?? "",
        Price = Convert.ToDecimal(row["cena"]),
        Status = row["status"]?.ToString() ?? "",
        StatusId = Convert.ToInt32(row["id_statusa"]),
        Courier = row["kurier"] is DBNull ? null : row["kurier"]?.ToString(),
        CourierId = row["id_kuriera"] is DBNull ? null : Convert.ToInt32(row["id_kuriera"]),
        CreatedAt = Convert.ToDateTime(row["data_sozdaniya"]),
        DeliveredAt = row["data_dostavki"] is DBNull ? null : Convert.ToDateTime(row["data_dostavki"]),
        Comment = row["kommentariy"] is DBNull ? null : row["kommentariy"]?.ToString(),
        PaymentMethod = row["sposob_oplaty"] is DBNull ? null : row["sposob_oplaty"]?.ToString()
    };

    private static void ExecuteProc(string name, params SqlParameter[] parameters)
    {
        using var connection = Db.Open();
        using var command = new SqlCommand(name, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };
        command.Parameters.AddRange(parameters);
        command.ExecuteNonQuery();
    }

    private static string? GetLogin(int? userId)
    {
        if (userId is null) return null;
        return Db.Scalar<string>(
            "SELECT login FROM dbo.KURS_KS_Polzovatel WHERE id_polzovatelya=@id",
            new SqlParameter("@id", userId.Value));
    }
}
