using Courier.API.Auth;
using Courier.Data;
using Courier.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Courier.API.Controllers;

[ApiController]
[Route("api/tariffs")]
[Authorize]
public class TariffsController : ControllerBase
{
    /// <summary>Тарифы по зонам и весу.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TariffDto>), StatusCodes.Status200OK)]
    public ActionResult<List<TariffDto>> GetTariffs([FromQuery] int? zoneId)
    {
        try
        {
            return Ok(ReferenceService.GetTariffsDto(zoneId));
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Добавление тарифа.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TariffDto), StatusCodes.Status201Created)]
    public ActionResult<TariffDto> CreateTariff([FromBody] TariffDto request)
    {
        try
        {
            ReferenceService.SaveTariff(null, request.ZoneId, request.WeightFrom, request.WeightTo, request.Price);
            var created = ReferenceService.GetTariffsDto().LastOrDefault();
            return Created($"/api/tariffs", created);
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Изменение тарифа.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult UpdateTariff(int id, [FromBody] TariffDto request)
    {
        try
        {
            ReferenceService.SaveTariff(id, request.ZoneId, request.WeightFrom, request.WeightTo, request.Price);
            return NoContent();
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }

    /// <summary>Удаление тарифа.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult DeleteTariff(int id)
    {
        try
        {
            ReferenceService.DeleteTariff(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return ApiErrors.From(ex);
        }
    }
}
