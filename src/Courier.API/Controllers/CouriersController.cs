using System.Data;
using Courier.API.Auth;
using Courier.Data;
using Courier.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Courier.API.Controllers;

[ApiController]
[Route("api/couriers")]
[Authorize]
public class CouriersController : ControllerBase
{
    /// <summary>Список курьеров с показателями загрузки.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CourierDto>), StatusCodes.Status200OK)]
    public ActionResult<List<CourierDto>> GetCouriers([FromQuery] string? search)
    {
        try
        {
            return Ok(ReferenceService.GetCouriersDto(search));
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Курьер по идентификатору.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CourierDto), StatusCodes.Status200OK)]
    public ActionResult<CourierDto> GetCourier(int id)
    {
        try
        {
            var courier = ReferenceService.GetCourierDto(id);
            return courier is null ? NotFound(new ApiError { Error = "Курьер не найден.", Code = "NOT_FOUND" }) : Ok(courier);
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Заказы курьера.</summary>
    [HttpGet("{id:int}/orders")]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    public ActionResult<List<OrderDto>> GetCourierOrders(int id)
    {
        if (User.IsCourier() && User.CourierId() != id) return Forbid();

        try
        {
            var table = OrderService.GetOrders(courierId: id);
            return Ok(table.Rows.Cast<DataRow>().Select(OrderService.MapOrder).ToList());
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Добавление курьера.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(CourierDto), StatusCodes.Status201Created)]
    public ActionResult<CourierDto> CreateCourier([FromBody] CourierDto request)
    {
        try
        {
            ReferenceService.SaveCourier(null, request.UserId, request.Fio, request.Telefon,
                request.Transport, request.ZoneId, string.IsNullOrWhiteSpace(request.Status) ? "Свободен" : request.Status,
                true);
            var created = ReferenceService.GetCouriersDto().FirstOrDefault(c => c.Telefon == request.Telefon);
            return CreatedAtAction(nameof(GetCourier), new { id = created?.Id ?? 0 }, created);
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Изменение курьера.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(CourierDto), StatusCodes.Status200OK)]
    public ActionResult<CourierDto> UpdateCourier(int id, [FromBody] CourierDto request)
    {
        try
        {
            ReferenceService.SaveCourier(id, request.UserId, request.Fio, request.Telefon,
                request.Transport, request.ZoneId, string.IsNullOrWhiteSpace(request.Status) ? "Свободен" : request.Status,
                request.IsActive);
            return Ok(ReferenceService.GetCourierDto(id));
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Архивирование курьера.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ArchiveCourier(int id)
    {
        try
        {
            ReferenceService.ArchiveCourier(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }
}
