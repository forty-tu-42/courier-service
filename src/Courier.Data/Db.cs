using System.Data;
using Microsoft.Data.SqlClient;

namespace Courier.Data;

/// <summary>
/// Единая точка подключения к MS SQL Server.
/// Строку подключения можно переопределить переменной окружения
/// COURIER_DB_CONNECTION или свойством Db.ConnectionString.
/// </summary>
public static class Db
{
    public const string DefaultConnectionString =
        "Server=stud-mssql.sttec.yar.ru,38325;Database=user93_db;User Id=user93_db;Password=user93;" +
        "TrustServerCertificate=True;Connection Timeout=20;Encrypt=False;";

    private static string _connectionString =
        Environment.GetEnvironmentVariable("COURIER_DB_CONNECTION") ?? DefaultConnectionString;

    public static string ConnectionString
    {
        get => _connectionString;
        set => _connectionString = string.IsNullOrWhiteSpace(value) ? DefaultConnectionString : value;
    }

    public static SqlConnection Open()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    public static DataTable Query(string sql, params SqlParameter[] parameters)
    {
        using var connection = Open();
        using var command = new SqlCommand(sql, connection) { CommandTimeout = 60 };
        if (parameters.Length > 0) command.Parameters.AddRange(parameters);
        using var adapter = new SqlDataAdapter(command);
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }

    public static int Execute(string sql, params SqlParameter[] parameters)
    {
        using var connection = Open();
        using var command = new SqlCommand(sql, connection) { CommandTimeout = 60 };
        if (parameters.Length > 0) command.Parameters.AddRange(parameters);
        return command.ExecuteNonQuery();
    }

    public static T? Scalar<T>(string sql, params SqlParameter[] parameters)
    {
        using var connection = Open();
        using var command = new SqlCommand(sql, connection) { CommandTimeout = 60 };
        if (parameters.Length > 0) command.Parameters.AddRange(parameters);
        var value = command.ExecuteScalar();
        if (value is null || value is DBNull) return default;
        return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
    }

    public static bool TestConnection(out string message)
    {
        try
        {
            using var connection = Open();
            using var command = new SqlCommand("SELECT DB_NAME()", connection);
            var name = command.ExecuteScalar()?.ToString();
            message = $"Подключено к базе {name}";
            return true;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return false;
        }
    }
}
