using System.Data;
using Courier.Shared;
using Microsoft.Data.SqlClient;

namespace Courier.Data;

/// <summary>Справочники и простые CRUD-операции.</summary>
public static partial class ReferenceService
{
    public static DataTable GetZones()
        => Db.Query("SELECT id_zony, nazvanie, opisanie FROM dbo.KURS_KS_Zona ORDER BY nazvanie");

    public static DataTable GetStatuses()
        => Db.Query("SELECT id_statusa, nazvanie FROM dbo.KURS_KS_Status_Zakaza ORDER BY id_statusa");

    public static DataTable GetClients(string? search = null)
    {
        var text = (search ?? "").Trim();
        return Db.Query(
            """
            SELECT id_klienta, fio, telefon, email, adres, is_active, data_registracii
            FROM dbo.KURS_KS_Klient
            WHERE (@search=N'' OR fio LIKE @like OR telefon LIKE @like OR ISNULL(email,'') LIKE @like)
            ORDER BY fio;
            """,
            new SqlParameter("@search", text),
            new SqlParameter("@like", "%" + text + "%"));
    }

    public static DataTable GetClient(int id)
        => Db.Query("SELECT * FROM dbo.KURS_KS_Klient WHERE id_klienta=@id", new SqlParameter("@id", id));

    public static int GetClientIdByPhone(string phone)
        => Db.Scalar<int>("SELECT id_klienta FROM dbo.KURS_KS_Klient WHERE telefon=@phone",
            new SqlParameter("@phone", phone.Trim()));

    public static DataTable GetCouriers(string? search = null)
    {
        var text = (search ?? "").Trim();
        return Db.Query(
            """
            SELECT v.id_kuriera, v.fio, v.telefon, v.transport, v.zona, v.status, v.is_active,
                   v.vsego_zakazov, v.dostavleno, v.v_rabote, v.summa_dostavki,
                   ku.id_zony, ku.id_polzovatelya
            FROM dbo.VW_KS_Zagruzka_Kurierov v
            JOIN dbo.KURS_KS_Kurier ku ON ku.id_kuriera = v.id_kuriera
            WHERE (@search=N'' OR v.fio LIKE @like OR v.telefon LIKE @like OR v.transport LIKE @like)
            ORDER BY v.fio;
            """,
            new SqlParameter("@search", text),
            new SqlParameter("@like", "%" + text + "%"));
    }

    public static DataTable GetCourier(int id)
        => Db.Query(
            """
            SELECT ku.id_kuriera, ku.id_polzovatelya, ku.fio, ku.telefon, ku.transport,
                   ku.id_zony, z.nazvanie AS zona, ku.status, ku.is_active, ku.data_priema
            FROM dbo.KURS_KS_Kurier ku
            JOIN dbo.KURS_KS_Zona z ON z.id_zony=ku.id_zony
            WHERE ku.id_kuriera=@id;
            """,
            new SqlParameter("@id", id));

    public static DataTable GetFreeCouriers(int? zoneId = null)
        => Db.Query(
            """
            SELECT ku.id_kuriera, ku.fio, ku.telefon, ku.transport,
                   z.nazvanie AS zona, ku.status
            FROM dbo.KURS_KS_Kurier ku
            JOIN dbo.KURS_KS_Zona z ON z.id_zony=ku.id_zony
            WHERE ku.is_active=1 AND ku.status=N'Свободен'
              AND (@zone IS NULL OR ku.id_zony=@zone)
            ORDER BY ku.fio;
            """,
            new SqlParameter("@zone", (object?)zoneId ?? DBNull.Value));

    public static DataTable GetTariffs(int? zoneId = null)
        => Db.Query(
            """
            SELECT t.id_tarifa, t.id_zony, z.nazvanie AS zona, t.ves_ot, t.ves_do, t.cena
            FROM dbo.KURS_KS_Tarif t
            JOIN dbo.KURS_KS_Zona z ON z.id_zony=t.id_zony
            WHERE (@zone IS NULL OR t.id_zony=@zone)
            ORDER BY z.nazvanie, t.ves_ot;
            """,
            new SqlParameter("@zone", (object?)zoneId ?? DBNull.Value));

    public static DataTable GetTariff(int id)
        => Db.Query("SELECT * FROM dbo.KURS_KS_Tarif WHERE id_tarifa=@id", new SqlParameter("@id", id));

    public static DataTable GetUsers(string? search = null)
    {
        var text = (search ?? "").Trim();
        return Db.Query(
            """
            SELECT p.id_polzovatelya, p.login, p.fio, p.telefon, p.is_active,
                   p.data_sozdaniya, r.id_roli, r.nazvanie AS rol
            FROM dbo.KURS_KS_Polzovatel p
            JOIN dbo.KURS_KS_Rol r ON r.id_roli=p.id_roli
            WHERE (@search=N'' OR p.login LIKE @like OR p.fio LIKE @like OR r.nazvanie LIKE @like)
            ORDER BY p.login;
            """,
            new SqlParameter("@search", text),
            new SqlParameter("@like", "%" + text + "%"));
    }

    public static DataTable GetUser(int id)
        => Db.Query(
            """
            SELECT p.*, r.nazvanie AS rol
            FROM dbo.KURS_KS_Polzovatel p
            JOIN dbo.KURS_KS_Rol r ON r.id_roli=p.id_roli
            WHERE p.id_polzovatelya=@id;
            """,
            new SqlParameter("@id", id));

    public static DataTable GetRoles()
        => Db.Query("SELECT id_roli, nazvanie FROM dbo.KURS_KS_Rol ORDER BY id_roli");

    public static DataTable GetAudit(string? search = null, int limit = 500)
    {
        var text = (search ?? "").Trim();
        return Db.Query(
            $"""
            SELECT TOP ({limit}) id_zapisi, login, deystvie, tablica, id_obekta, data_deystviya
            FROM dbo.KURS_KS_Audit
            WHERE (@search=N'' OR ISNULL(login,'') LIKE @like OR deystvie LIKE @like OR ISNULL(tablica,'') LIKE @like)
            ORDER BY data_deystviya DESC, id_zapisi DESC;
            """,
            new SqlParameter("@search", text),
            new SqlParameter("@like", "%" + text + "%"));
    }

    // ---------------- Клиенты ----------------

    public static void SaveClient(int? id, string fio, string telefon, string? email, string? adres, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(fio)) throw new InvalidOperationException("Укажите ФИО клиента.");
        if (string.IsNullOrWhiteSpace(telefon)) throw new InvalidOperationException("Укажите телефон клиента.");

        if (id is null)
        {
            Db.Execute(
                """
                INSERT INTO dbo.KURS_KS_Klient (fio, telefon, email, adres, is_active)
                VALUES (@fio, @tel, @email, @adres, @active);
                """,
                new SqlParameter("@fio", fio.Trim()),
                new SqlParameter("@tel", telefon.Trim()),
                new SqlParameter("@email", (object?)email ?? DBNull.Value),
                new SqlParameter("@adres", (object?)adres ?? DBNull.Value),
                new SqlParameter("@active", isActive));
        }
        else
        {
            Db.Execute(
                """
                UPDATE dbo.KURS_KS_Klient
                SET fio=@fio, telefon=@tel, email=@email, adres=@adres, is_active=@active
                WHERE id_klienta=@id;
                """,
                new SqlParameter("@fio", fio.Trim()),
                new SqlParameter("@tel", telefon.Trim()),
                new SqlParameter("@email", (object?)email ?? DBNull.Value),
                new SqlParameter("@adres", (object?)adres ?? DBNull.Value),
                new SqlParameter("@active", isActive),
                new SqlParameter("@id", id.Value));
        }
    }

    public static void ArchiveClient(int id)
    {
        Db.Execute("UPDATE dbo.KURS_KS_Klient SET is_active=0 WHERE id_klienta=@id",
            new SqlParameter("@id", id));
    }

    // ---------------- Курьеры ----------------

    public static void SaveCourier(int? id, int? userId, string fio, string telefon,
        string transport, int zoneId, string status, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(fio)) throw new InvalidOperationException("Укажите ФИО курьера.");
        if (string.IsNullOrWhiteSpace(telefon)) throw new InvalidOperationException("Укажите телефон курьера.");

        if (id is null)
        {
            Db.Execute(
                """
                INSERT INTO dbo.KURS_KS_Kurier
                    (id_polzovatelya, fio, telefon, transport, id_zony, status, is_active)
                VALUES (@user, @fio, @tel, @transport, @zone, @status, @active);
                """,
                new SqlParameter("@user", (object?)userId ?? DBNull.Value),
                new SqlParameter("@fio", fio.Trim()),
                new SqlParameter("@tel", telefon.Trim()),
                new SqlParameter("@transport", transport),
                new SqlParameter("@zone", zoneId),
                new SqlParameter("@status", status),
                new SqlParameter("@active", isActive));
        }
        else
        {
            Db.Execute(
                """
                UPDATE dbo.KURS_KS_Kurier
                SET id_polzovatelya=@user, fio=@fio, telefon=@tel,
                    transport=@transport, id_zony=@zone, status=@status, is_active=@active
                WHERE id_kuriera=@id;
                """,
                new SqlParameter("@user", (object?)userId ?? DBNull.Value),
                new SqlParameter("@fio", fio.Trim()),
                new SqlParameter("@tel", telefon.Trim()),
                new SqlParameter("@transport", transport),
                new SqlParameter("@zone", zoneId),
                new SqlParameter("@status", status),
                new SqlParameter("@active", isActive),
                new SqlParameter("@id", id.Value));
        }
    }

    public static void ArchiveCourier(int id)
    {
        var active = Db.Scalar<int>(
            """
            SELECT COUNT(*) FROM dbo.KURS_KS_Zakaz z
            JOIN dbo.KURS_KS_Status_Zakaza s ON s.id_statusa=z.id_statusa
            WHERE z.id_kuriera=@id AND s.nazvanie IN (N'Назначен', N'В пути');
            """,
            new SqlParameter("@id", id));

        if (active > 0)
            throw new InvalidOperationException("У курьера есть активные заказы. Сначала завершите или переназначьте их.");

        Db.Execute("UPDATE dbo.KURS_KS_Kurier SET is_active=0, status=N'Отдыхает' WHERE id_kuriera=@id",
            new SqlParameter("@id", id));
    }

    // ---------------- Тарифы ----------------

    public static void SaveTariff(int? id, int zoneId, decimal from, decimal to, decimal price)
    {
        if (to <= from) throw new InvalidOperationException("Верхняя граница веса должна быть больше нижней.");
        if (price < 0) throw new InvalidOperationException("Стоимость не может быть отрицательной.");

        if (id is null)
        {
            Db.Execute(
                "INSERT INTO dbo.KURS_KS_Tarif (id_zony, ves_ot, ves_do, cena) VALUES (@zone, @from, @to, @price);",
                new SqlParameter("@zone", zoneId),
                new SqlParameter("@from", from),
                new SqlParameter("@to", to),
                new SqlParameter("@price", price));
        }
        else
        {
            Db.Execute(
                """
                UPDATE dbo.KURS_KS_Tarif
                SET id_zony=@zone, ves_ot=@from, ves_do=@to, cena=@price
                WHERE id_tarifa=@id;
                """,
                new SqlParameter("@zone", zoneId),
                new SqlParameter("@from", from),
                new SqlParameter("@to", to),
                new SqlParameter("@price", price),
                new SqlParameter("@id", id.Value));
        }
    }

    public static void DeleteTariff(int id)
    {
        var used = Db.Scalar<int>("SELECT COUNT(*) FROM dbo.KURS_KS_Zakaz WHERE id_tarifa=@id",
            new SqlParameter("@id", id));
        if (used > 0)
            throw new InvalidOperationException("Тариф используется в заказах и не может быть удалён.");

        Db.Execute("DELETE FROM dbo.KURS_KS_Tarif WHERE id_tarifa=@id", new SqlParameter("@id", id));
    }

    // ---------------- Пользователи ----------------

    public static void SaveUser(int? id, string login, string? password, int roleId,
        string fio, string? telefon, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(login)) throw new InvalidOperationException("Укажите логин.");
        if (login.Trim().Length < 3) throw new InvalidOperationException("Логин должен быть не короче 3 символов.");
        if (string.IsNullOrWhiteSpace(fio)) throw new InvalidOperationException("Укажите ФИО пользователя.");

        if (id is null)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                throw new InvalidOperationException("Пароль должен быть не короче 6 символов.");

            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.Hash(password, salt);
            Db.Execute(
                """
                INSERT INTO dbo.KURS_KS_Polzovatel (login, parol_hash, sol, id_roli, fio, telefon, is_active)
                VALUES (@login, @hash, @salt, @role, @fio, @tel, @active);
                """,
                new SqlParameter("@login", login.Trim()),
                new SqlParameter("@hash", hash),
                new SqlParameter("@salt", salt),
                new SqlParameter("@role", roleId),
                new SqlParameter("@fio", fio.Trim()),
                new SqlParameter("@tel", (object?)telefon ?? DBNull.Value),
                new SqlParameter("@active", isActive));
        }
        else
        {
            Db.Execute(
                """
                UPDATE dbo.KURS_KS_Polzovatel
                SET login=@login, id_roli=@role, fio=@fio, telefon=@tel, is_active=@active
                WHERE id_polzovatelya=@id;
                """,
                new SqlParameter("@login", login.Trim()),
                new SqlParameter("@role", roleId),
                new SqlParameter("@fio", fio.Trim()),
                new SqlParameter("@tel", (object?)telefon ?? DBNull.Value),
                new SqlParameter("@active", isActive),
                new SqlParameter("@id", id.Value));

            if (!string.IsNullOrWhiteSpace(password))
            {
                if (password.Length < 6)
                    throw new InvalidOperationException("Пароль должен быть не короче 6 символов.");
                AuthService.ChangePassword(id.Value, password);
            }
        }
    }

    public static void ArchiveUser(int id, int currentUserId)
    {
        if (id == currentUserId)
            throw new InvalidOperationException("Нельзя заблокировать самого себя.");

        Db.Execute("UPDATE dbo.KURS_KS_Polzovatel SET is_active=0 WHERE id_polzovatelya=@id",
            new SqlParameter("@id", id));
    }
}
