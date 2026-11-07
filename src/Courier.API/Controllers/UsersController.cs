using Courier.API.Auth;
using Courier.Data;
using Courier.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Courier.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    /// <summary>Список пользователей системы.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    public ActionResult<List<UserDto>> GetUsers([FromQuery] string? search)
    {
        try
        {
            return Ok(ReferenceService.GetUsersDto(search));
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Пользователь по идентификатору.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public ActionResult<UserDto> GetUser(int id)
    {
        try
        {
            var user = ReferenceService.GetUserDto(id);
            return user is null ? NotFound(new ApiError { Error = "Пользователь не найден.", Code = "NOT_FOUND" }) : Ok(user);
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Создание пользователя.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    public ActionResult<UserDto> CreateUser([FromBody] SaveUserRequest request)
    {
        try
        {
            ReferenceService.SaveUser(null, request.Login, request.Password, request.RoleId,
                request.Fio, request.Phone, true);
            var created = ReferenceService.GetUsersDto().FirstOrDefault(u => u.Login == request.Login);
            return CreatedAtAction(nameof(GetUser), new { id = created?.Id ?? 0 }, created);
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Изменение пользователя.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public ActionResult<UserDto> UpdateUser(int id, [FromBody] SaveUserRequest request)
    {
        try
        {
            ReferenceService.SaveUser(id, request.Login, request.Password, request.RoleId,
                request.Fio, request.Phone, request.IsActive);
            return Ok(ReferenceService.GetUserDto(id));
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Блокировка пользователя.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult BlockUser(int id)
    {
        try
        {
            ReferenceService.ArchiveUser(id, User.UserId() ?? 0);
            return NoContent();
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }
}
