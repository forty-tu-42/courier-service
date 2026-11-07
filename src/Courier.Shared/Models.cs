namespace Courier.Shared;

/// <summary>Текущий пользователь, вошедший в систему.</summary>
public sealed class UserSession
{
    public int UserId { get; init; }
    public string Login { get; init; } = "";
    public string Role { get; init; } = "";       // русское название роли
    public string RoleCode { get; init; } = "";   // Admin / Manager / Courier
    public string Fio { get; init; } = "";
    public int? CourierId { get; init; }

    public bool IsAdmin => RoleCode == "Admin";
    public bool IsManager => RoleCode == "Manager";
    public bool IsCourier => RoleCode == "Courier";
    public bool CanEditOrders => IsAdmin || IsManager;
    public bool CanManageReferences => IsAdmin || IsManager;
    public bool CanManageUsers => IsAdmin;
    public bool CanViewAudit => IsAdmin;
}

public sealed class ClientDto
{
    public int Id { get; set; }
    public string Fio { get; set; } = "";
    public string Telefon { get; set; } = "";
    public string? Email { get; set; }
    public string? Adres { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CourierDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Fio { get; set; } = "";
    public string Telefon { get; set; } = "";
    public string Transport { get; set; } = "";
    public int ZoneId { get; set; }
    public string Zona { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int OrdersTotal { get; set; }
    public int OrdersDelivered { get; set; }
}

public sealed class OrderDto
{
    public int Id { get; set; }
    public string Nomer { get; set; } = "";
    public int ClientId { get; set; }
    public string Client { get; set; } = "";
    public string ClientPhone { get; set; } = "";
    public string SenderAddress { get; set; } = "";
    public string RecipientFio { get; set; } = "";
    public string RecipientPhone { get; set; } = "";
    public string RecipientAddress { get; set; } = "";
    public decimal Weight { get; set; }
    public int ZoneId { get; set; }
    public string Zone { get; set; } = "";
    public int? TariffId { get; set; }
    public decimal Price { get; set; }
    public int StatusId { get; set; }
    public string Status { get; set; } = "";
    public int? CourierId { get; set; }
    public string? Courier { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? Comment { get; set; }
    public string? PaymentMethod { get; set; }
}

public sealed class TariffDto
{
    public int Id { get; set; }
    public int ZoneId { get; set; }
    public string Zone { get; set; } = "";
    public decimal WeightFrom { get; set; }
    public decimal WeightTo { get; set; }
    public decimal Price { get; set; }
}

public sealed class StatusDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class ZoneDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public sealed class UserDto
{
    public int Id { get; set; }
    public string Login { get; set; } = "";
    public string Fio { get; set; } = "";
    public string Role { get; set; } = "";
    public int RoleId { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class HistoryDto
{
    public int Id { get; set; }
    public int StatusId { get; set; }
    public string Status { get; set; } = "";
    public string? User { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Comment { get; set; }
}

public sealed class DashboardDto
{
    public int OrdersToday { get; set; }
    public int OrdersInWork { get; set; }
    public int DeliveredThisMonth { get; set; }
    public int FreeCouriers { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public int ClientsTotal { get; set; }
    public List<OrderDto> RecentOrders { get; set; } = new();
}

public sealed class DeliveryReportDto
{
    public string Nomer { get; set; } = "";
    public string Client { get; set; } = "";
    public string? Courier { get; set; }
    public string Zone { get; set; } = "";
    public decimal Weight { get; set; }
    public decimal Price { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

public sealed class CourierLoadDto
{
    public int CourierId { get; set; }
    public string Fio { get; set; } = "";
    public string Transport { get; set; } = "";
    public string Zone { get; set; } = "";
    public string Status { get; set; } = "";
    public int Total { get; set; }
    public int Delivered { get; set; }
    public int InWork { get; set; }
    public decimal DeliveredSum { get; set; }
}

public sealed class RevenueDto
{
    public string Zone { get; set; } = "";
    public string Period { get; set; } = "";
    public int Orders { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class StatusReportDto
{
    public string Status { get; set; } = "";
    public int Count { get; set; }
    public decimal Sum { get; set; }
}

public sealed class MonthlyRevenueDto
{
    public string Period { get; set; } = "";
    public int Orders { get; set; }
    public decimal Revenue { get; set; }
}

// ---------- Контракты API ----------

public sealed class LoginRequest
{
    public string Login { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class LoginResponse
{
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public UserSession User { get; set; } = new();
}

public sealed class CreateOrderRequest
{
    public int ClientId { get; set; }
    public string SenderAddress { get; set; } = "";
    public string RecipientFio { get; set; } = "";
    public string RecipientPhone { get; set; } = "";
    public string RecipientAddress { get; set; } = "";
    public decimal Weight { get; set; }
    public int ZoneId { get; set; }
    public string? Comment { get; set; }
    public string? PaymentMethod { get; set; } = "Наличные";
    public int? CourierId { get; set; }
}

public sealed class UpdateOrderRequest
{
    public int ClientId { get; set; }
    public string SenderAddress { get; set; } = "";
    public string RecipientFio { get; set; } = "";
    public string RecipientPhone { get; set; } = "";
    public string RecipientAddress { get; set; } = "";
    public decimal Weight { get; set; }
    public int ZoneId { get; set; }
    public string? Comment { get; set; }
    public string? PaymentMethod { get; set; }
    public int? CourierId { get; set; }
}

public sealed class AssignCourierRequest
{
    public int CourierId { get; set; }
}

public sealed class ChangeStatusRequest
{
    public int StatusId { get; set; }
    public string? Comment { get; set; }
}

public sealed class OrderCreatedResponse
{
    public int Id { get; set; }
    public string Nomer { get; set; } = "";
    public int? TariffId { get; set; }
    public decimal Price { get; set; }
}

public sealed class ApiError
{
    public string Error { get; set; } = "";
    public string? Code { get; set; }
    public string? Detail { get; set; }
}

public sealed class SaveUserRequest
{
    public string Login { get; set; } = "";
    public string? Password { get; set; }
    public int RoleId { get; set; }
    public string Fio { get; set; } = "";
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
}
