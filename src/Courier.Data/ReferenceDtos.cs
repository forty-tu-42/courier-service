using System.Data;
using Courier.Shared;

namespace Courier.Data;

public static partial class ReferenceService
{
    public static List<ClientDto> GetClientsDto(string? search = null)
        => GetClients(search).Rows.Cast<DataRow>().Select(MapClient).ToList();

    public static ClientDto? GetClientDto(int id)
    {
        var table = GetClient(id);
        return table.Rows.Count == 0 ? null : MapClient(table.Rows[0]);
    }

    public static List<CourierDto> GetCouriersDto(string? search = null)
        => GetCouriers(search).Rows.Cast<DataRow>().Select(MapCourier).ToList();

    public static CourierDto? GetCourierDto(int id)
    {
        var table = GetCourier(id);
        return table.Rows.Count == 0 ? null : MapCourier(table.Rows[0]);
    }

    public static List<TariffDto> GetTariffsDto(int? zoneId = null)
        => GetTariffs(zoneId).Rows.Cast<DataRow>().Select(MapTariff).ToList();

    public static List<ZoneDto> GetZonesDto()
        => GetZones().Rows.Cast<DataRow>().Select(r => new ZoneDto
        {
            Id = Convert.ToInt32(r["id_zony"]),
            Name = r["nazvanie"]?.ToString() ?? "",
            Description = r["opisanie"] is DBNull ? null : r["opisanie"]?.ToString()
        }).ToList();

    public static List<StatusDto> GetStatusesDto()
        => GetStatuses().Rows.Cast<DataRow>().Select(r => new StatusDto
        {
            Id = Convert.ToInt32(r["id_statusa"]),
            Name = r["nazvanie"]?.ToString() ?? ""
        }).ToList();

    public static List<UserDto> GetUsersDto(string? search = null)
        => GetUsers(search).Rows.Cast<DataRow>().Select(MapUser).ToList();

    public static UserDto? GetUserDto(int id)
    {
        var table = GetUser(id);
        return table.Rows.Count == 0 ? null : MapUser(table.Rows[0]);
    }

    private static ClientDto MapClient(DataRow r) => new()
    {
        Id = Convert.ToInt32(r["id_klienta"]),
        Fio = r["fio"]?.ToString() ?? "",
        Telefon = r["telefon"]?.ToString() ?? "",
        Email = r.Table.Columns.Contains("email") && r["email"] is not DBNull ? r["email"]?.ToString() : null,
        Adres = r.Table.Columns.Contains("adres") && r["adres"] is not DBNull ? r["adres"]?.ToString() : null,
        IsActive = r["is_active"] is bool b && b
    };

    private static CourierDto MapCourier(DataRow r) => new()
    {
        Id = Convert.ToInt32(r["id_kuriera"]),
        UserId = r.Table.Columns.Contains("id_polzovatelya") && r["id_polzovatelya"] is not DBNull
            ? Convert.ToInt32(r["id_polzovatelya"]) : null,
        Fio = r["fio"]?.ToString() ?? "",
        Telefon = r["telefon"]?.ToString() ?? "",
        Transport = r["transport"]?.ToString() ?? "",
        ZoneId = r.Table.Columns.Contains("id_zony") && r["id_zony"] is not DBNull
            ? Convert.ToInt32(r["id_zony"]) : 0,
        Zona = r["zona"]?.ToString() ?? "",
        Status = r["status"]?.ToString() ?? "",
        IsActive = r["is_active"] is bool active && active,
        OrdersTotal = r.Table.Columns.Contains("vsego_zakazov") ? Convert.ToInt32(r["vsego_zakazov"]) : 0,
        OrdersDelivered = r.Table.Columns.Contains("dostavleno") ? Convert.ToInt32(r["dostavleno"]) : 0
    };

    private static TariffDto MapTariff(DataRow r) => new()
    {
        Id = Convert.ToInt32(r["id_tarifa"]),
        ZoneId = Convert.ToInt32(r["id_zony"]),
        Zone = r["zona"]?.ToString() ?? "",
        WeightFrom = Convert.ToDecimal(r["ves_ot"]),
        WeightTo = Convert.ToDecimal(r["ves_do"]),
        Price = Convert.ToDecimal(r["cena"])
    };

    private static UserDto MapUser(DataRow r) => new()
    {
        Id = Convert.ToInt32(r["id_polzovatelya"]),
        Login = r["login"]?.ToString() ?? "",
        Fio = r["fio"]?.ToString() ?? "",
        Role = r["rol"]?.ToString() ?? "",
        RoleId = Convert.ToInt32(r["id_roli"]),
        Phone = r["telefon"] is DBNull ? null : r["telefon"]?.ToString(),
        IsActive = r["is_active"] is bool b && b,
        CreatedAt = Convert.ToDateTime(r["data_sozdaniya"])
    };
}
