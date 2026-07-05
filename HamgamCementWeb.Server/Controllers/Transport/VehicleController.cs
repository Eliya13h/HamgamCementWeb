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
[Route("api/transport/vehicles")]
[Authorize]
public class VehicleController : TransportControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(Vehicle.Code),
        [2] = nameof(Vehicle.PlateNumber),
        [4] = nameof(Vehicle.Brand),
        [5] = nameof(Vehicle.ModelYear),
        [7] = nameof(Vehicle.IsActive),
    };

    public VehicleController(AppDbContext db) : base(db)
    {
    }

    [HttpPost("datatable")]
    [HasPermission("transport.vehicles.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.Vehicles
            .AsNoTracking()
            .Where(v => v.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(v =>
                v.Code.Contains(searchValue) ||
                v.PlateNumber.Contains(searchValue) ||
                v.Brand.Contains(searchValue) ||
                (v.VehicleType != null && v.VehicleType.Name.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(Vehicle.CreatedAt))
            .Skip(start)
            .Take(length)
            .Select(v => new
            {
                vehicleId = v.VehicleID,
                code = v.Code,
                plateNumber = v.PlateNumber,
                vehicleTypeId = v.VehicleTypeId,
                vehicleTypeName = v.VehicleType != null ? v.VehicleType.Name : string.Empty,
                brand = v.Brand,
                modelYear = v.ModelYear,
                color = v.Color,
                chassisNumber = v.ChassisNumber,
                engineNumber = v.EngineNumber,
                fuelTankCapacity = v.FuelTankCapacity,
                description = v.Description,
                defaultDriverId = v.DefaultDriverId,
                defaultDriverName = v.DefaultDriver != null
                    ? v.DefaultDriver.Name + " " + v.DefaultDriver.Family
                    : null,
                vehicleOwnerId = v.VehicleOwnerId,
                vehicleOwnerName = v.Owner != null ? v.Owner.Name + " " + v.Owner.Family : null,
                isActive = v.IsActive == true,
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
                r.vehicleId,
                r.code,
                r.plateNumber,
                r.vehicleTypeId,
                r.vehicleTypeName,
                r.brand,
                r.modelYear,
                r.color,
                r.chassisNumber,
                r.engineNumber,
                r.fuelTankCapacity,
                r.description,
                r.defaultDriverId,
                r.defaultDriverName,
                r.vehicleOwnerId,
                r.vehicleOwnerName,
                r.isActive,
            }),
        });
    }

    // لیست وسایل نقلیه برای دراپ‌داون‌ها
    [HttpGet("list")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await Db.Vehicles
            .AsNoTracking()
            .Where(v => v.IsDeleted != true && v.IsActive == true)
            .OrderBy(v => v.Code)
            .Select(v => new
            {
                value = v.VehicleID,
                label = v.Code + " — " + v.PlateNumber,
                defaultDriverId = v.DefaultDriverId,
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost]
    [HasPermission("transport.vehicles.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveVehicleRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var conflict = await CheckUniqueness(request, null, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        if (request.VehicleOwnerId is > 0)
        {
            var ownerExists = await Db.VehicleOwners
                .AnyAsync(o => o.VehicleOwnerID == request.VehicleOwnerId && o.IsDeleted != true, cancellationToken);
            if (!ownerExists)
            {
                return NotFound(new { message = "صاحب وسیله نقلیه انتخاب‌شده یافت نشد." });
            }
        }

        var vehicle = new Vehicle
        {
            Code = $"TMP{DateTime.UtcNow.Ticks}",
            PlateNumber = request.PlateNumber.Trim(),
            VehicleTypeId = request.VehicleTypeId,
            Brand = request.Brand?.Trim() ?? string.Empty,
            ModelYear = request.ModelYear,
            Color = request.Color?.Trim(),
            ChassisNumber = request.ChassisNumber?.Trim(),
            EngineNumber = request.EngineNumber?.Trim(),
            FuelTankCapacity = request.FuelTankCapacity,
            Description = request.Description?.Trim(),
            VehicleOwnerId = request.VehicleOwnerId,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };

        Db.Vehicles.Add(vehicle);
        await Db.SaveChangesAsync(cancellationToken);

        vehicle.Code = TransportCodeHelper.ForVehicle(vehicle.VehicleID);

        var driverError = await VehicleDriverService.AssignDefaultDriverAsync(
            Db, vehicle, request.DefaultDriverId, cancellationToken);
        if (driverError is not null)
        {
            return driverError;
        }

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "وسیله نقلیه با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
    [HasPermission("transport.vehicles.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveVehicleRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var vehicle = await Db.Vehicles
            .FirstOrDefaultAsync(v => v.VehicleID == id && v.IsDeleted != true, cancellationToken);
        if (vehicle is null)
        {
            return NotFound(new { message = "وسیله نقلیه یافت نشد." });
        }

        var conflict = await CheckUniqueness(request, id, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        vehicle.PlateNumber = request.PlateNumber.Trim();
        vehicle.VehicleTypeId = request.VehicleTypeId;
        vehicle.Brand = request.Brand?.Trim() ?? string.Empty;
        vehicle.ModelYear = request.ModelYear;
        vehicle.Color = request.Color?.Trim();
        vehicle.ChassisNumber = request.ChassisNumber?.Trim();
        vehicle.EngineNumber = request.EngineNumber?.Trim();
        vehicle.FuelTankCapacity = request.FuelTankCapacity;
        vehicle.Description = request.Description?.Trim();
        vehicle.VehicleOwnerId = request.VehicleOwnerId;
        vehicle.IsActive = request.IsActive;
        vehicle.IsUpdated = true;
        vehicle.UpdatedAt = DateTime.Now;
        vehicle.UpdatedBy = ResolveCurrentUserId();

        if (request.VehicleOwnerId is > 0)
        {
            var ownerExists = await Db.VehicleOwners
                .AnyAsync(o => o.VehicleOwnerID == request.VehicleOwnerId && o.IsDeleted != true, cancellationToken);
            if (!ownerExists)
            {
                return NotFound(new { message = "صاحب وسیله نقلیه انتخاب‌شده یافت نشد." });
            }
        }

        var driverError = await VehicleDriverService.AssignDefaultDriverAsync(
            Db, vehicle, request.DefaultDriverId, cancellationToken);
        if (driverError is not null)
        {
            return driverError;
        }

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "وسیله نقلیه با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("transport.vehicles.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var vehicle = await Db.Vehicles
            .FirstOrDefaultAsync(v => v.VehicleID == id && v.IsDeleted != true, cancellationToken);
        if (vehicle is null)
        {
            return NotFound(new { message = "وسیله نقلیه یافت نشد." });
        }

        vehicle.IsDeleted = true;
        vehicle.IsActive = false;
        vehicle.DeletedAt = DateTime.Now;
        vehicle.DeletedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "وسیله نقلیه با موفقیت حذف شد." });
    }

    private async Task<IActionResult?> CheckUniqueness(
        SaveVehicleRequest request,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var plate = request.PlateNumber.Trim();

        var plateExists = await Db.Vehicles.AnyAsync(
            v => v.IsDeleted != true && v.PlateNumber == plate && (excludeId == null || v.VehicleID != excludeId),
            cancellationToken);
        if (plateExists)
        {
            return Conflict(new { message = "وسیله نقلیه با این پلاک قبلاً ثبت شده است." });
        }

        var typeExists = await Db.VehicleTypes.AnyAsync(
            t => t.VehicleTypeID == request.VehicleTypeId && t.IsDeleted != true,
            cancellationToken);
        if (!typeExists)
        {
            return NotFound(new { message = "نوع وسیله نقلیه انتخاب‌شده یافت نشد." });
        }

        return null;
    }

    public class SaveVehicleRequest
    {
        [Required(ErrorMessage = "شماره پلاک الزامی است.")]
        [MaxLength(50)]
        public string PlateNumber { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "نوع وسیله نقلیه الزامی است.")]
        public int VehicleTypeId { get; set; }

        public int? DefaultDriverId { get; set; }

        public int? VehicleOwnerId { get; set; }

        [MaxLength(200)]
        public string? Brand { get; set; }

        public int? ModelYear { get; set; }

        [MaxLength(50)]
        public string? Color { get; set; }

        [MaxLength(100)]
        public string? ChassisNumber { get; set; }

        [MaxLength(100)]
        public string? EngineNumber { get; set; }

        public decimal? FuelTankCapacity { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
