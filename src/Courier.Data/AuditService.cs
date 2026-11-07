using Microsoft.Data.SqlClient;

namespace Courier.Data;

public static class AuditService
{
    public static void Log(string? login, string action, string? table = null, int? objectId = null)
    {
        try
        {
            Db.Execute(
                """
                INSERT INTO dbo.KURS_KS_Audit (login, deystvie, tablica, id_obekta)
                VALUES (@login, @action, @table, @id)
                """,
                new SqlParameter("@login", (object?)login ?? DBNull.Value),
                new SqlParameter("@action", action),
                new SqlParameter("@table", (object?)table ?? DBNull.Value),
                new SqlParameter("@id", (object?)objectId ?? DBNull.Value));
        }
        catch
        {
            // Аудит не должен ломать основную операцию.
        }
    }
}
