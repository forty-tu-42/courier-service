using Courier.API.Auth;
using Courier.Data;
using Courier.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Courier.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,Manager")]
public class ReportsController : ControllerBase
{
    /// <summary>Отчёт о доставках за период.</summary>
    [HttpGet("deliveries")]
    [ProducesResponseType(typeof(List<DeliveryReportDto>), StatusCodes.Status200OK)]
    public ActionResult<List<DeliveryReportDto>> Deliveries([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        try
        {
            var (start, end) = Normalize(from, to);
            return Ok(ReportService.GetDeliveriesDto(start, end));
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Количество и сумма заказов по статусам за период.</summary>
    [HttpGet("statuses")]
    [ProducesResponseType(typeof(List<StatusReportDto>), StatusCodes.Status200OK)]
    public ActionResult<List<StatusReportDto>> DeliveriesByStatus([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        try
        {
            var (start, end) = Normalize(from, to);
            return Ok(ReportService.GetOrdersByStatusDto(start, end));
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Загрузка курьеров.</summary>
    [HttpGet("courier-load")]
    [ProducesResponseType(typeof(List<CourierLoadDto>), StatusCodes.Status200OK)]
    public ActionResult<List<CourierLoadDto>> CourierLoad()
    {
        try
        {
            return Ok(ReportService.GetCourierLoadDto());
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Выручка по зонам за период.</summary>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(List<RevenueDto>), StatusCodes.Status200OK)]
    public ActionResult<List<RevenueDto>> Revenue([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        try
        {
            var (start, end) = Normalize(from, to);
            return Ok(ReportService.GetRevenueDto(start, end));
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Выручка по месяцам за период.</summary>
    [HttpGet("monthly")]
    [ProducesResponseType(typeof(List<MonthlyRevenueDto>), StatusCodes.Status200OK)]
    public ActionResult<List<MonthlyRevenueDto>> Monthly([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        try
        {
            var (start, end) = Normalize(from, to);
            return Ok(ReportService.GetMonthlyRevenueDto(start, end));
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    private static (DateTime Start, DateTime End) Normalize(DateTime? from, DateTime? to)
    {
        var start = from?.Date ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var end = to?.Date ?? DateTime.Today;
        return end < start ? (end, start) : (start, end);
    }
}
