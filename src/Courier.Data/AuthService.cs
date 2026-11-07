using Courier.Shared;
using Microsoft.Data.SqlClient;

namespace Courier.Data;

public static class AuthService
{
    public static string ToRoleCode(string roleName) => roleName switch
    {
        "Администратор" => "Admin",
        "Менеджер" => "Manager",
        "Курьер" => "Courier",
        _ => roleName
    };

    /// <summary>Проверяет логин и пароль, возвращает сессию или null.</summary>
    public static UserSession? Login(string login, string password, bool writeAudit = true)
    {
        var table = Db.Query(
            """
            SELECT p.id_polzovatelya, p.login, p.parol_hash, p.sol, p.fio,
                   r.nazvanie AS role_name,
                   ku.id_kuriera
            FROM dbo.KURS_KS_Polzovatel p
            JOIN dbo.KURS_KS_Rol r ON r.id_roli = p.id_roli
            LEFT JOIN dbo.KURS_KS_Kurier ku ON ku.id_polzovatelya = p.id_polzovatelya
            WHERE p.login = @login AND p.is_active = 1
            """,
            new SqlParameter("@login", login.Trim()));

        if (table.Rows.Count == 0) return null;

        var row = table.Rows[0];
        var salt = row["sol"]?.ToString() ?? "";
        var hash = row["parol_hash"]?.ToString() ?? "";
        if (!PasswordHasher.Verify(password, salt, hash)) return null;

        var roleName = row["role_name"]?.ToString() ?? "";
        var session = new UserSession
        {
            UserId = Convert.ToInt32(row["id_polzovatelya"]),
            Login = row["login"]?.ToString() ?? login,
            Role = roleName,
            RoleCode = ToRoleCode(roleName),
            Fio = row["fio"]?.ToString() ?? "",
            CourierId = row["id_kuriera"] is DBNull or null ? null : Convert.ToInt32(row["id_kuriera"])
        };

        if (writeAudit)
            AuditService.Log(session.Login, "Вход в систему", "KURS_KS_Polzovatel", session.UserId);

        return session;
    }

    public static UserSession? GetSession(int userId)
    {
        var table = Db.Query(
            """
            SELECT p.id_polzovatelya, p.login, p.fio, r.nazvanie AS role_name, ku.id_kuriera
            FROM dbo.KURS_KS_Polzovatel p
            JOIN dbo.KURS_KS_Rol r ON r.id_roli = p.id_roli
            LEFT JOIN dbo.KURS_KS_Kurier ku ON ku.id_polzovatelya = p.id_polzovatelya
            WHERE p.id_polzovatelya = @id AND p.is_active = 1;
            """,
            new SqlParameter("@id", userId));

        if (table.Rows.Count == 0) return null;
        var row = table.Rows[0];
        var roleName = row["role_name"]?.ToString() ?? "";
        return new UserSession
        {
            UserId = Convert.ToInt32(row["id_polzovatelya"]),
            Login = row["login"]?.ToString() ?? "",
            Role = roleName,
            RoleCode = ToRoleCode(roleName),
            Fio = row["fio"]?.ToString() ?? "",
            CourierId = row["id_kuriera"] is DBNull ? null : Convert.ToInt32(row["id_kuriera"])
        };
    }

    public static void ChangePassword(int userId, string newPassword)
    {
        var salt = PasswordHasher.GenerateSalt();
        var hash = PasswordHasher.Hash(newPassword, salt);
        Db.Execute(
            "UPDATE dbo.KURS_KS_Polzovatel SET parol_hash=@hash, sol=@salt WHERE id_polzovatelya=@id",
            new SqlParameter("@hash", hash),
            new SqlParameter("@salt", salt),
            new SqlParameter("@id", userId));
    }
}
