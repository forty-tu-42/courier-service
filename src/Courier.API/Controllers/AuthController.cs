using Courier.API.Auth;
using Courier.Data;
using Courier.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Courier.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config) => _config = config;

    /// <summary>Вход в систему и выдача JWT-токена.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new ApiError { Error = "Введите логин и пароль.", Code = "VALIDATION_ERROR" });

        try
        {
            var session = AuthService.Login(request.Login, request.Password);
            if (session is null)
                return Unauthorized(new ApiError
                {
                    Error = "Неверный логин или пароль, либо учётная запись заблокирована.",
                    Code = "INVALID_CREDENTIALS"
                });

            var (token, expiresAt) = JwtService.Create(session, _config);
            return Ok(new LoginResponse { Token = token, ExpiresAt = expiresAt, User = session });
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Информация о текущем пользователе по токену.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserSession), StatusCodes.Status200OK)]
    public ActionResult<UserSession> Me()
    {
        var session = AuthService.GetSession(User.UserId() ?? 0);
        return session is null ? NotFound() : Ok(session);
    }
}
