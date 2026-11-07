using Courier.API.Auth;
using Courier.Data;
using Courier.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Courier.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    /// <summary>Сводные показатели для панели управления.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public ActionResult<DashboardDto> Get()
    {
        try
        {
            return Ok(OrderService.GetDashboard());
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }
}
