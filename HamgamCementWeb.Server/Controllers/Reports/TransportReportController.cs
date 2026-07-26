using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HamgamCementWeb.Server.Controllers.Reports;

[ApiController]
[Route("api/reports/transport")]
[Authorize]
public class TransportReportController : ControllerBase
{
    private readonly ITransportReportService _reports;

    public TransportReportController(ITransportReportService reports)
    {
        _reports = reports;
    }

    [HttpGet("summary")]
    [HasPermission("reporting.transport.view")]
    public async Task<IActionResult> Summary(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var summary = await _reports.GetSummaryAsync(fromDate, toDate, cancellationToken);
        return Ok(new
        {
            totalTrips = summary.TotalTrips,
            totalWeightTon = summary.TotalWeightTon,
            totalTripRevenue = summary.TotalTripRevenue,
            ownFleetTrips = summary.OwnFleetTrips,
            hiredTrips = summary.HiredTrips,
            byVehicle = summary.ByVehicle.Select(r => new
            {
                vehicleId = r.VehicleId,
                vehicleLabel = r.VehicleLabel,
                freightMode = r.FreightMode,
                freightModeName = r.FreightMode == 1 ? "خودی" : r.FreightMode == 2 ? "کرایه‌ای" : "—",
                tripCount = r.TripCount,
                totalWeightTon = r.TotalWeightTon,
                totalRevenue = r.TotalRevenue,
                purchaseFreightAmount = r.PurchaseFreightAmount,
                saleFreightAmount = r.SaleFreightAmount,
            }),
            byPurpose = summary.ByPurpose.Select(r => new
            {
                tripPurpose = r.TripPurpose,
                tripPurposeName = r.TripPurpose switch
                {
                    1 => "ورود خرید",
                    2 => "تحویل فروش",
                    _ => "باربری تجاری",
                },
                tripCount = r.TripCount,
                totalWeightTon = r.TotalWeightTon,
                totalRevenue = r.TotalRevenue,
            }),
            maintenance = summary.Maintenance.Select(r => new
            {
                vehicleId = r.VehicleId,
                vehicleLabel = r.VehicleLabel,
                maintenanceCost = r.MaintenanceCost,
                partsCost = r.PartsCost,
                depreciationCost = r.DepreciationCost,
                totalCost = r.MaintenanceCost + r.PartsCost + r.DepreciationCost,
            }),
        });
    }
}
