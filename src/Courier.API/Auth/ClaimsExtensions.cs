using System.Security.Claims;
using Courier.Shared;

namespace Courier.API.Auth;

public static class ClaimsExtensions
{
    public static int? UserId(this ClaimsPrincipal principal)
        => int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    public static int? CourierId(this ClaimsPrincipal principal)
        => int.TryParse(principal.FindFirst("courierId")?.Value, out var id) ? id : null;

    public static string RoleCode(this ClaimsPrincipal principal)
        => principal.FindFirst(ClaimTypes.Role)?.Value ?? "";

    public static bool IsCourier(this ClaimsPrincipal principal)
        => principal.RoleCode() == "Courier";

    public static bool CanManageOrders(this ClaimsPrincipal principal)
        => principal.RoleCode() is "Admin" or "Manager";

    public static bool CanAccessOrder(this ClaimsPrincipal principal, OrderDto order)
    {
        if (principal.CanManageOrders()) return true;
        return principal.IsCourier() && principal.CourierId() == order.CourierId;
    }
}
