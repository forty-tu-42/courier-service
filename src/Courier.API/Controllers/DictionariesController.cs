using Courier.API.Auth;
using Courier.Data;
using Courier.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Courier.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class DictionariesController : ControllerBase
{
    /// <summary>Статусы заказов.</summary>
    [HttpGet("statuses")]
    [ProducesResponseType(typeof(List<StatusDto>), StatusCodes.Status200OK)]
    public ActionResult<List<StatusDto>> GetStatuses()
    {
        try
        {
            return Ok(ReferenceService.GetStatusesDto());
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Зоны обслуживания.</summary>
    [HttpGet("zones")]
    [ProducesResponseType(typeof(List<ZoneDto>), StatusCodes.Status200OK)]
    public ActionResult<List<ZoneDto>> GetZones()
    {
        try
        {
            return Ok(ReferenceService.GetZonesDto());
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }
}
