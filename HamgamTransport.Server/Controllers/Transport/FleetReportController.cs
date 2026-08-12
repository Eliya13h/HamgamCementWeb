using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HamgamTransport.Server.Controllers.Transport;

[ApiController]
[Route("api/transport/reports")]
[Authorize]
public class FleetReportController : ControllerBase
{
    private readonly IFleetReportService _reports;

    public FleetReportController(IFleetReportService reports)
    {
        _reports = reports;
    }

    [HttpGet("vehicle-pl")]
    public async Task<IActionResult> VehiclePl([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var rows = await _reports.GetVehiclePlAsync(from, to, ct);
        return Ok(rows);
    }

    [HttpGet("owner-balances")]
    public async Task<IActionResult> OwnerBalances(CancellationToken ct)
    {
        var rows = await _reports.GetOwnerBalancesAsync(ct);
        return Ok(rows);
    }

    [HttpGet("customer-ar")]
    public async Task<IActionResult> CustomerAr(CancellationToken ct)
    {
        var rows = await _reports.GetCustomerArAsync(ct);
        return Ok(rows);
    }
}
