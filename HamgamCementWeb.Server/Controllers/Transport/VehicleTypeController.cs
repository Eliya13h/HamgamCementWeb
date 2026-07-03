using System.ComponentModel.DataAnnotations;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Transport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Transport;

[ApiController]
[Route("api/transport/vehicle-types")]
[Authorize]
public class VehicleTypeController : TransportControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(VehicleType.Name),
        [4] = nameof(VehicleType.IsActive),
    };

    public VehicleTypeController(AppDbContext db) : base(db)
    {
    }

    [HttpPost("datatable")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.VehicleTypes
            .AsNoTracking()
            .Where(t => t.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(t =>
                t.Name.Contains(searchValue) ||
                (t.Description != null && t.Description.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(VehicleType.CreatedAt))
            .Skip(start)
            .Take(length)
            .Select(t => new
            {
                vehicleTypeId = t.VehicleTypeID,
                name = t.Name,
                description = t.Description,
                vehiclesCount = t.Vehicles.Count(v => v.IsDeleted != true),
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
                r.vehicleTypeId,
                r.name,
                r.description,
                r.vehiclesCount,
                r.isActive,
            }),
        });
    }

    // لیست انواع وسایل برای استفاده در دراپ‌داون‌ها
    [HttpGet("list")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await Db.VehicleTypes
            .AsNoTracking()
            .Where(t => t.IsDeleted != true && t.IsActive == true)
            .OrderBy(t => t.Name)
            .Select(t => new { value = t.VehicleTypeID, label = t.Name })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveVehicleTypeRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var name = request.Name.Trim();
        var exists = await Db.VehicleTypes
            .AnyAsync(t => t.IsDeleted != true && t.Name == name, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "نوع وسیله نقلیه با این نام قبلاً ثبت شده است." });
        }

        Db.VehicleTypes.Add(new VehicleType
        {
            Name = name,
            Description = request.Description?.Trim(),
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        });

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "نوع وسیله نقلیه با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveVehicleTypeRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = await Db.VehicleTypes
            .FirstOrDefaultAsync(t => t.VehicleTypeID == id && t.IsDeleted != true, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "نوع وسیله نقلیه یافت نشد." });
        }

        var name = request.Name.Trim();
        var exists = await Db.VehicleTypes
            .AnyAsync(t => t.IsDeleted != true && t.Name == name && t.VehicleTypeID != id, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "نوع وسیله نقلیه با این نام قبلاً ثبت شده است." });
        }

        entity.Name = name;
        entity.Description = request.Description?.Trim();
        entity.IsActive = request.IsActive;
        entity.IsUpdated = true;
        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "نوع وسیله نقلیه با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await Db.VehicleTypes
            .FirstOrDefaultAsync(t => t.VehicleTypeID == id && t.IsDeleted != true, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "نوع وسیله نقلیه یافت نشد." });
        }

        var inUse = await Db.Vehicles
            .AnyAsync(v => v.VehicleTypeId == id && v.IsDeleted != true, cancellationToken);
        if (inUse)
        {
            return Conflict(new { message = "این نوع وسیله نقلیه دارای وسیله ثبت‌شده است و قابل حذف نیست." });
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = DateTime.Now;
        entity.DeletedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "نوع وسیله نقلیه با موفقیت حذف شد." });
    }

    public class SaveVehicleTypeRequest
    {
        [Required(ErrorMessage = "نام الزامی است.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
