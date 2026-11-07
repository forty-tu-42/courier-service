using System.Data;
using Courier.API.Auth;
using Courier.Data;
using Courier.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Courier.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    /// <summary>Журнал заказов с фильтрами. Курьер видит только свои заказы.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    public ActionResult<List<OrderDto>> GetOrders(
        [FromQuery] string? search,
        [FromQuery] int? statusId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? courierId)
    {
        try
        {
            if (User.IsCourier())
            {
                courierId = User.CourierId();
                if (courierId is null) return Forbid();
            }

            var table = OrderService.GetOrders(search, statusId, from, to, courierId);
            return Ok(table.Rows.Cast<DataRow>().Select(OrderService.MapOrder).ToList());
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Заказ по идентификатору.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<OrderDto> GetOrder(int id)
    {
        try
        {
            var order = OrderService.GetOrderDto(id);
            if (order is null) return NotFound(new ApiError { Error = "Заказ не найден.", Code = "NOT_FOUND" });
            if (!User.CanAccessOrder(order))
                return Forbid();
            return Ok(order);
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>История изменения статусов заказа.</summary>
    [HttpGet("{id:int}/history")]
    [ProducesResponseType(typeof(List<HistoryDto>), StatusCodes.Status200OK)]
    public ActionResult<List<HistoryDto>> GetHistory(int id)
    {
        try
        {
            var order = OrderService.GetOrderDto(id);
            if (order is null) return NotFound(new ApiError { Error = "Заказ не найден.", Code = "NOT_FOUND" });
            if (!User.CanAccessOrder(order)) return Forbid();
            return Ok(OrderService.GetHistoryDto(id));
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Создание заказа. Стоимость рассчитывается по зоне и весу.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(OrderCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<OrderCreatedResponse> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var validation = Validate(request.ClientId, request.SenderAddress, request.RecipientFio,
            request.RecipientPhone, request.RecipientAddress, request.Weight, request.ZoneId);
        if (validation is not null)
            return BadRequest(new ApiError { Error = validation, Code = "VALIDATION_ERROR" });

        try
        {
            var created = OrderService.Create(request, User.UserId());
            var order = OrderService.GetOrderDto(created.Id);
            return CreatedAtAction(nameof(GetOrder), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Изменение заказа (кроме завершённых и отменённых).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public ActionResult<OrderDto> UpdateOrder(int id, [FromBody] UpdateOrderRequest request)
    {
        var validation = Validate(request.ClientId, request.SenderAddress, request.RecipientFio,
            request.RecipientPhone, request.RecipientAddress, request.Weight, request.ZoneId);
        if (validation is not null)
            return BadRequest(new ApiError { Error = validation, Code = "VALIDATION_ERROR" });

        try
        {
            OrderService.Update(id, request, User.UserId());
            return Ok(OrderService.GetOrderDto(id));
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Назначение курьера на заказ.</summary>
    [HttpPost("{id:int}/assign")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public ActionResult<OrderDto> AssignCourier(int id, [FromBody] AssignCourierRequest request)
    {
        if (request.CourierId <= 0)
            return BadRequest(new ApiError { Error = "Укажите курьера.", Code = "VALIDATION_ERROR" });

        try
        {
            OrderService.AssignCourier(id, request.CourierId, User.UserId());
            return Ok(OrderService.GetOrderDto(id));
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Смена статуса заказа. Курьер может менять только свои заказы.</summary>
    [HttpPost("{id:int}/status")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public ActionResult<OrderDto> ChangeStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        try
        {
            var order = OrderService.GetOrderDto(id);
            if (order is null) return NotFound(new ApiError { Error = "Заказ не найден.", Code = "NOT_FOUND" });
            if (!User.CanAccessOrder(order)) return Forbid();

            OrderService.ChangeStatus(id, request.StatusId, User.UserId(), request.Comment);
            return Ok(OrderService.GetOrderDto(id));
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Отмена заказа (мягкое удаление).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult CancelOrder(int id)
    {
        try
        {
            OrderService.Cancel(id, User.UserId());
            return NoContent();
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    private static string? Validate(int clientId, string sender, string recipientFio,
        string recipientPhone, string recipientAddress, decimal weight, int zoneId)
    {
        if (clientId <= 0) return "Выберите клиента.";
        if (string.IsNullOrWhiteSpace(sender)) return "Укажите адрес отправителя.";
        if (string.IsNullOrWhiteSpace(recipientFio)) return "Укажите ФИО получателя.";
        if (string.IsNullOrWhiteSpace(recipientPhone) || recipientPhone.Length < 10)
            return "Укажите корректный телефон получателя.";
        if (string.IsNullOrWhiteSpace(recipientAddress)) return "Укажите адрес доставки.";
        if (weight <= 0) return "Вес должен быть больше нуля.";
        if (zoneId <= 0) return "Выберите зону обслуживания.";
        return null;
    }
}
