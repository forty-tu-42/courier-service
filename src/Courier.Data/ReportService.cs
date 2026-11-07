using System.Data;
using Courier.Shared;
using Microsoft.Data.SqlClient;

namespace Courier.Data;

public static class ReportService
{
    public static DataTable GetDeliveries(DateTime from, DateTime to)
        => Db.Query(
            "EXEC dbo.sp_KS_Otchet_Dostavki @data_ot=@from, @data_do=@to;",
            new SqlParameter("@from", from.Date),
            new SqlParameter("@to", to.Date));

    public static DataTable GetOrdersByStatus(DateTime from, DateTime to)
        => Db.Query(
            """
            SELECT s.nazvanie AS status, COUNT(z.id_zakaza) AS kolichestvo,
                   ISNULL(SUM(z.cena), 0) AS summa
            FROM dbo.KURS_KS_Zakaz z
            JOIN dbo.KURS_KS_Status_Zakaza s ON s.id_statusa=z.id_statusa
            WHERE CAST(z.data_sozdaniya AS date) BETWEEN @from AND @to
            GROUP BY s.nazvanie
            ORDER BY kolichestvo DESC;
            """,
            new SqlParameter("@from", from.Date),
            new SqlParameter("@to", to.Date));

    public static DataTable GetCourierLoad()
        => Db.Query(
            """
            SELECT id_kuriera, fio, transport, zona, status, vsego_zakazov, dostavleno, v_rabote, summa_dostavki
            FROM dbo.VW_KS_Zagruzka_Kurierov
            ORDER BY dostavleno DESC, vsego_zakazov DESC;
            """);

    public static DataTable GetRevenueByZone(DateTime from, DateTime to)
        => Db.Query(
            """
            SELECT zn.nazvanie AS zona, COUNT(*) AS zakazov,
                   ISNULL(SUM(z.cena), 0) AS vyruchka,
                   ISNULL(AVG(z.cena), 0) AS sredniy_chek
            FROM dbo.KURS_KS_Zakaz z
            JOIN dbo.KURS_KS_Zona zn ON zn.id_zony=z.id_zony
            JOIN dbo.KURS_KS_Status_Zakaza s ON s.id_statusa=z.id_statusa
            WHERE s.nazvanie <> N'Отменён'
              AND CAST(z.data_sozdaniya AS date) BETWEEN @from AND @to
            GROUP BY zn.nazvanie
            ORDER BY vyruchka DESC;
            """,
            new SqlParameter("@from", from.Date),
            new SqlParameter("@to", to.Date));

    public static DataTable GetMonthlyRevenue(DateTime from, DateTime to)
        => Db.Query(
            """
            SELECT FORMAT(z.data_sozdaniya, 'yyyy-MM') AS period,
                   COUNT(*) AS zakazov,
                   ISNULL(SUM(z.cena), 0) AS vyruchka
            FROM dbo.KURS_KS_Zakaz z
            JOIN dbo.KURS_KS_Status_Zakaza s ON s.id_statusa=z.id_statusa
            WHERE s.nazvanie <> N'Отменён'
              AND CAST(z.data_sozdaniya AS date) BETWEEN @from AND @to
            GROUP BY FORMAT(z.data_sozdaniya, 'yyyy-MM')
            ORDER BY period;
            """,
            new SqlParameter("@from", from.Date),
            new SqlParameter("@to", to.Date));

    // ---------- Типизированные версии для API ----------

    public static List<DeliveryReportDto> GetDeliveriesDto(DateTime from, DateTime to)
        => GetDeliveries(from, to).Rows.Cast<DataRow>().Select(r => new DeliveryReportDto
        {
            Nomer = r["nomer"]?.ToString() ?? "",
            Client = r["klient"]?.ToString() ?? "",
            Courier = r["kurier"] is DBNull ? null : r["kurier"]?.ToString(),
            Zone = r["zona"]?.ToString() ?? "",
            Weight = Convert.ToDecimal(r["ves"]),
            Price = Convert.ToDecimal(r["cena"]),
            PaymentMethod = r["sposob_oplaty"] is DBNull ? null : r["sposob_oplaty"]?.ToString(),
            CreatedAt = Convert.ToDateTime(r["data_sozdaniya"]),
            DeliveredAt = r["data_dostavki"] is DBNull ? null : Convert.ToDateTime(r["data_dostavki"])
        }).ToList();

    public static List<StatusReportDto> GetOrdersByStatusDto(DateTime from, DateTime to)
        => GetOrdersByStatus(from, to).Rows.Cast<DataRow>().Select(r => new StatusReportDto
        {
            Status = r["status"]?.ToString() ?? "",
            Count = Convert.ToInt32(r["kolichestvo"]),
            Sum = Convert.ToDecimal(r["summa"])
        }).ToList();

    public static List<MonthlyRevenueDto> GetMonthlyRevenueDto(DateTime from, DateTime to)
        => GetMonthlyRevenue(from, to).Rows.Cast<DataRow>().Select(r => new MonthlyRevenueDto
        {
            Period = r["period"]?.ToString() ?? "",
            Orders = Convert.ToInt32(r["zakazov"]),
            Revenue = Convert.ToDecimal(r["vyruchka"])
        }).ToList();

    public static List<CourierLoadDto> GetCourierLoadDto()
        => GetCourierLoad().Rows.Cast<DataRow>().Select(r => new CourierLoadDto
        {
            CourierId = Convert.ToInt32(r["id_kuriera"]),
            Fio = r["fio"]?.ToString() ?? "",
            Transport = r["transport"]?.ToString() ?? "",
            Zone = r["zona"]?.ToString() ?? "",
            Status = r["status"]?.ToString() ?? "",
            Total = Convert.ToInt32(r["vsego_zakazov"]),
            Delivered = Convert.ToInt32(r["dostavleno"]),
            InWork = Convert.ToInt32(r["v_rabote"]),
            DeliveredSum = Convert.ToDecimal(r["summa_dostavki"])
        }).ToList();

    public static List<RevenueDto> GetRevenueDto(DateTime from, DateTime to)
        => GetRevenueByZone(from, to).Rows.Cast<DataRow>().Select(r => new RevenueDto
        {
            Zone = r["zona"]?.ToString() ?? "",
            Period = $"{from:yyyy-MM} — {to:yyyy-MM}",
            Orders = Convert.ToInt32(r["zakazov"]),
            Revenue = Convert.ToDecimal(r["vyruchka"])
        }).ToList();
}
