using Courier.API.Auth;
using Courier.Data;
using Courier.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Courier.API.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize(Roles = "Admin,Manager")]
public class ClientsController : ControllerBase
{
    /// <summary>Список клиентов с поиском по ФИО, телефону и e-mail.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ClientDto>), StatusCodes.Status200OK)]
    public ActionResult<List<ClientDto>> GetClients([FromQuery] string? search)
    {
        try
        {
            return Ok(ReferenceService.GetClientsDto(search));
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Клиент по идентификатору.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    public ActionResult<ClientDto> GetClient(int id)
    {
        try
        {
            var client = ReferenceService.GetClientDto(id);
            return client is null ? NotFound(new ApiError { Error = "Клиент не найден.", Code = "NOT_FOUND" }) : Ok(client);
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Добавление клиента.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status201Created)]
    public ActionResult<ClientDto> CreateClient([FromBody] ClientDto request)
    {
        try
        {
            ReferenceService.SaveClient(null, request.Fio, request.Telefon, request.Email, request.Adres, true);
            var created = ReferenceService.GetClientsDto().FirstOrDefault(c => c.Telefon == request.Telefon);
            return CreatedAtAction(nameof(GetClient), new { id = created?.Id ?? 0 }, created);
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Изменение клиента.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    public ActionResult<ClientDto> UpdateClient(int id, [FromBody] ClientDto request)
    {
        try
        {
            ReferenceService.SaveClient(id, request.Fio, request.Telefon, request.Email, request.Adres, request.IsActive);
            return Ok(ReferenceService.GetClientDto(id));
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Архивирование клиента.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ArchiveClient(int id)
    {
        try
        {
            ReferenceService.ArchiveClient(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }
}
