using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Courier.Shared;
using Microsoft.IdentityModel.Tokens;

namespace Courier.API.Auth;

public static class JwtService
{
    public static (string Token, DateTime ExpiresAt) Create(UserSession user, IConfiguration config)
    {
        var key = config["Jwt:Key"]
                  ?? "COURIER_SERVICE_DEVELOPMENT_KEY_2026_CHANGE_ME_1234567890";
        var issuer = config["Jwt:Issuer"] ?? "CourierService";
        var audience = config["Jwt:Audience"] ?? "CourierServiceClient";
        var minutes = int.TryParse(config["Jwt:LifetimeMinutes"], out var m) ? m : 120;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Login),
            new(ClaimTypes.GivenName, user.Fio),
            new(ClaimTypes.Role, user.RoleCode),
            new("roleName", user.Role),
            new("courierId", user.CourierId?.ToString() ?? "")
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(minutes);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
