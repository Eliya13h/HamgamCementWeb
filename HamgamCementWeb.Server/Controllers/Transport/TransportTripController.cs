using System.ComponentModel.DataAnnotations;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Transport;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Transport;

[ApiController]
[Route("api/transport/trips")]
[Authorize]
public class TransportTripController : TransportControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(TransportTrip.TripNumber),
        [4] = nameof(TransportTrip.DepartureDate),
        [5] = nameof(TransportTrip.ArrivalDate),
        [6] = nameof(TransportTrip.CargoWeightTon),
        [7] = nameof(TransportTrip.TripRevenue),
        [8] = nameof(TransportTrip.Status),
    };

    public TransportTripController(AppDbContext db) : base(db)
    {
    }

    [HttpPost("datatable")]
    [HasPermission("transport.shipping.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.TransportTrips
            .AsNoTracking()
            .Where(t => t.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(t =>
                t.TripNumber.Contains(searchValue) ||
                (t.CargoDescription != null && t.CargoDescription.Contains(searchValue)) ||
                (t.Vehicle != null && (t.Vehicle.Code.Contains(searchValue) || t.Vehicle.PlateNumber.Contains(searchValue))) ||
                (t.Route != null && t.Route.Name.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(TransportTrip.CreatedAt))
            .Skip(start)
            .Take(length)
            .Select(t => new
            {
                transportTripId = t.TransportTripID,
                tripNumber = t.TripNumber,
                vehicleId = t.VehicleId,
                vehicleLabel = t.Vehicle != null ? t.Vehicle.Code + " — " + t.Vehicle.PlateNumber : string.Empty,
                transportRouteId = t.TransportRouteId,
                routeName = t.Route != null ? t.Route.Name : string.Empty,
                driverId = t.DriverId,
                driverName = t.Driver != null
                    ? t.Driver.Name + " " + t.Driver.Family
                    : (t.Vehicle != null && t.Vehicle.DefaultDriver != null
                        ? t.Vehicle.DefaultDriver.Name + " " + t.Vehicle.DefaultDriver.Family
                        : null),
                effectiveDriverId = t.DriverId ?? (t.Vehicle != null ? t.Vehicle.DefaultDriverId : null),
                cargoDescription = t.CargoDescription,
                cargoWeightTon = t.CargoWeightTon,
                departureDate = t.DepartureDate,
                arrivalDate = t.ArrivalDate,
                fuelConsumedLiters = t.FuelConsumedLiters,
                odometerStart = t.OdometerStart,
                odometerEnd = t.OdometerEnd,
                tripRevenue = t.TripRevenue,
                status = (int)t.Status,
                description = t.Description,
                isActive = t.IsActive == true,
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select((r, i) => new
            {
                rowNumber = start + i + 1,
                r.transportTripId,
                r.tripNumber,
                r.vehicleId,
                r.vehicleLabel,
                r.transportRouteId,
                r.routeName,
                r.driverId,
                r.driverName,
                r.effectiveDriverId,
                r.cargoDescription,
                r.cargoWeightTon,
                r.departureDate,
                r.arrivalDate,
                r.fuelConsumedLiters,
                r.odometerStart,
                r.odometerEnd,
                r.tripRevenue,
                r.status,
                statusName = StatusName((TripStatus)r.status),
                r.description,
                r.isActive,
            }),
        });
    }

    // لیست سفرها برای دراپ‌داون‌ها (فاکتور و حوادث)
    [HttpGet("list")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await Db.TransportTrips
            .AsNoTracking()
            .Where(t => t.IsDeleted != true)
            .OrderByDescending(t => t.DepartureDate)
            .Select(t => new
            {
                value = t.TransportTripID,
                label = t.TripNumber + (t.Route != null ? " — " + t.Route.Name : string.Empty),
                vehicleId = t.VehicleId,
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost]
    [HasPermission("transport.shipping.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveTripRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var validationError = await ValidateTripAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var trip = new TransportTrip
        {
            TripNumber = $"TMP{DateTime.UtcNow.Ticks}",
            VehicleId = request.VehicleId,
            TransportRouteId = request.TransportRouteId,
            DriverId = request.DriverId,
            CargoDescription = request.CargoDescription?.Trim(),
            CargoWeightTon = request.CargoWeightTon,
            DepartureDate = request.DepartureDate,
            ArrivalDate = request.ArrivalDate,
            FuelConsumedLiters = request.FuelConsumedLiters,
            OdometerStart = request.OdometerStart,
            OdometerEnd = request.OdometerEnd,
            TripRevenue = request.TripRevenue,
            Status = request.Status,
            Description = request.Description?.Trim(),
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };

        Db.TransportTrips.Add(trip);
        await Db.SaveChangesAsync(cancellationToken);

        trip.TripNumber = TransportCodeHelper.ForTrip(trip.TransportTripID);
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "سفر با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
    [HasPermission("transport.shipping.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveTripRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var trip = await Db.TransportTrips
            .FirstOrDefaultAsync(t => t.TransportTripID == id && t.IsDeleted != true, cancellationToken);
        if (trip is null)
        {
            return NotFound(new { message = "سفر یافت نشد." });
        }

        var validationError = await ValidateTripAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        trip.VehicleId = request.VehicleId;
        trip.TransportRouteId = request.TransportRouteId;
        trip.DriverId = request.DriverId;
        trip.CargoDescription = request.CargoDescription?.Trim();
        trip.CargoWeightTon = request.CargoWeightTon;
        trip.DepartureDate = request.DepartureDate;
        trip.ArrivalDate = request.ArrivalDate;
        trip.FuelConsumedLiters = request.FuelConsumedLiters;
        trip.OdometerStart = request.OdometerStart;
        trip.OdometerEnd = request.OdometerEnd;
        trip.TripRevenue = request.TripRevenue;
        trip.Status = request.Status;
        trip.Description = request.Description?.Trim();
        trip.IsUpdated = true;
        trip.UpdatedAt = DateTime.Now;
        trip.UpdatedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "سفر با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("transport.shipping.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var trip = await Db.TransportTrips
            .FirstOrDefaultAsync(t => t.TransportTripID == id && t.IsDeleted != true, cancellationToken);
        if (trip is null)
        {
            return NotFound(new { message = "سفر یافت نشد." });
        }

        trip.IsDeleted = true;
        trip.IsActive = false;
        trip.DeletedAt = DateTime.Now;
        trip.DeletedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "سفر با موفقیت حذف شد." });
    }

    // اعتبارسنجی کلیدهای خارجی و سازگاری منطقی مقادیر سفر.
    private async Task<string?> ValidateTripAsync(SaveTripRequest request, CancellationToken cancellationToken)
    {
        var vehicleExists = await Db.Vehicles
            .AnyAsync(v => v.VehicleID == request.VehicleId && v.IsDeleted != true, cancellationToken);
        if (!vehicleExists)
        {
            return "وسیله نقلیه انتخاب‌شده یافت نشد.";
        }

        var routeExists = await Db.TransportRoutes
            .AnyAsync(r => r.TransportRouteID == request.TransportRouteId && r.IsDeleted != true, cancellationToken);
        if (!routeExists)
        {
            return "مسیر انتخاب‌شده یافت نشد.";
        }

        if (request.DriverId is > 0)
        {
            var driverExists = await Db.Drivers
                .AnyAsync(d => d.DriverID == request.DriverId && d.IsDeleted != true, cancellationToken);
            if (!driverExists)
            {
                return "راننده انتخاب‌شده یافت نشد.";
            }
        }

        if (request.ArrivalDate is DateTime arrival && arrival < request.DepartureDate)
        {
            return "تاریخ رسیدن نمی‌تواند پیش از تاریخ حرکت باشد.";
        }

        if (request.OdometerStart is decimal start &&
            request.OdometerEnd is decimal end &&
            end < start)
        {
            return "کیلومترشمار پایان نمی‌تواند کمتر از کیلومترشمار شروع باشد.";
        }

        return null;
    }

    private static string StatusName(TripStatus status) => status switch
    {
        TripStatus.Planned => "برنامه‌ریزی شده",
        TripStatus.InTransit => "در مسیر",
        TripStatus.Completed => "تکمیل شده",
        TripStatus.Cancelled => "لغو شده",
        _ => string.Empty,
    };

    public class SaveTripRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "انتخاب وسیله نقلیه الزامی است.")]
        public int VehicleId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "انتخاب مسیر الزامی است.")]
        public int TransportRouteId { get; set; }

        // راننده سفر — اگر خالی باشد از راننده پیش‌فرض وسیله استفاده می‌شود
        public int? DriverId { get; set; }

        [MaxLength(500)]
        public string? CargoDescription { get; set; }

        public decimal? CargoWeightTon { get; set; }

        [Required(ErrorMessage = "تاریخ حرکت الزامی است.")]
        public DateTime DepartureDate { get; set; }

        public DateTime? ArrivalDate { get; set; }

        public decimal? FuelConsumedLiters { get; set; }

        public decimal? OdometerStart { get; set; }

        public decimal? OdometerEnd { get; set; }

        public TripStatus Status { get; set; } = TripStatus.Planned;

        [Range(0, double.MaxValue, ErrorMessage = "درآمد سفر نامعتبر است.")]
        public decimal TripRevenue { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
    }
}
