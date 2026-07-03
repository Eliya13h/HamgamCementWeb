using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Controllers.Transport;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Inventory;

public abstract class InventoryControllerBase : ControllerBase
{
    protected readonly AppDbContext Db;

    protected InventoryControllerBase(AppDbContext db)
    {
        Db = db;
    }

    protected int? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    protected static string GetWarehouseTypeLabel(WarehouseType type) => type switch
    {
        WarehouseType.RawMaterials => "انبار مواد خام",
        WarehouseType.SemiFinished => "انبار مواد نیمه‌خام",
        WarehouseType.Processed => "انبار مواد پردازش‌شده",
        _ => type.ToString(),
    };
}

[ApiController]
[Route("api/inventory/warehouses")]
[Authorize]
public class WarehouseController : InventoryControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(Warehouse.Name),
        [2] = nameof(Warehouse.WarehouseType),
        [3] = nameof(Warehouse.Location),
        [4] = nameof(Warehouse.Capacity),
        [5] = "ProductsCount",
        [6] = nameof(Warehouse.IsActive),
    };

    public WarehouseController(AppDbContext db) : base(db)
    {
    }

    [HttpPost("datatable")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.Warehouses
            .AsNoTracking()
            .Where(w => w.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(w =>
                w.Name.Contains(searchValue) ||
                (w.Location != null && w.Location.Contains(searchValue)) ||
                (w.Description != null && w.Description.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(Warehouse.CreatedAt))
            .Skip(start)
            .Take(length)
            .Select(w => new
            {
                warehouseId = w.WarehouseID,
                name = w.Name,
                warehouseType = w.WarehouseType,
                location = w.Location,
                description = w.Description,
                capacity = w.Capacity,
                capacityMeaurmentId = w.CapacityMeaurmentId,
                capacityMeaurmentName = w.CapacityMeaurment != null ? w.CapacityMeaurment.Name : null,
                productsCount = w.Stocks.Count(s => s.IsDeleted != true && s.QuantityInBase > 0),
                isActive = w.IsActive == true,
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
                r.warehouseId,
                r.name,
                warehouseType = (int)r.warehouseType,
                warehouseTypeLabel = GetWarehouseTypeLabel(r.warehouseType),
                r.location,
                r.description,
                r.capacity,
                r.capacityMeaurmentId,
                r.capacityMeaurmentName,
                capacityText = r.capacity.HasValue
                    ? $"{r.capacity:N2} {r.capacityMeaurmentName ?? ""}".Trim()
                    : null,
                r.productsCount,
                r.isActive,
            }),
        });
    }

    [HttpGet("list")]
    public async Task<IActionResult> List(
        [FromQuery] string? types,
        CancellationToken cancellationToken)
    {
        var query = Db.Warehouses
            .AsNoTracking()
            .Where(w => w.IsDeleted != true && w.IsActive == true);

        if (!string.IsNullOrWhiteSpace(types))
        {
            var typeValues = types
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => Enum.TryParse<WarehouseType>(t, out var wt) ? (WarehouseType?)wt : null)
                .Where(t => t.HasValue)
                .Select(t => t!.Value)
                .ToList();

            if (typeValues.Count > 0)
            {
                query = query.Where(w => typeValues.Contains(w.WarehouseType));
            }
        }

        var items = await query
            .OrderBy(w => w.Name)
            .Select(w => new
            {
                value = w.WarehouseID,
                label = w.Name,
                warehouseType = (int)w.WarehouseType,
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveWarehouseRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var name = request.Name.Trim();
        var exists = await Db.Warehouses
            .AnyAsync(w => w.IsDeleted != true && w.Name == name, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "انبار با این نام قبلاً ثبت شده است." });
        }

        Db.Warehouses.Add(new Warehouse
        {
            Name = name,
            WarehouseType = request.WarehouseType,
            Location = request.Location?.Trim(),
            Description = request.Description?.Trim(),
            Capacity = request.Capacity,
            CapacityMeaurmentId = request.CapacityMeaurmentId,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        });

        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "انبار با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveWarehouseRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = await Db.Warehouses
            .FirstOrDefaultAsync(w => w.WarehouseID == id && w.IsDeleted != true, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "انبار یافت نشد." });
        }

        var name = request.Name.Trim();
        var exists = await Db.Warehouses
            .AnyAsync(w => w.IsDeleted != true && w.Name == name && w.WarehouseID != id, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "انبار با این نام قبلاً ثبت شده است." });
        }

        entity.Name = name;
        entity.WarehouseType = request.WarehouseType;
        entity.Location = request.Location?.Trim();
        entity.Description = request.Description?.Trim();
        entity.Capacity = request.Capacity;
        entity.CapacityMeaurmentId = request.CapacityMeaurmentId;
        entity.IsActive = request.IsActive;
        entity.IsUpdated = true;
        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "انبار با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await Db.Warehouses
            .FirstOrDefaultAsync(w => w.WarehouseID == id && w.IsDeleted != true, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "انبار یافت نشد." });
        }

        var hasStock = await Db.InventoryStocks
            .AnyAsync(s => s.WarehouseId == id && s.IsDeleted != true && s.QuantityInBase > 0, cancellationToken);
        if (hasStock)
        {
            return Conflict(new { message = "این انبار دارای موجودی است و قابل حذف نیست." });
        }

        var hasStocktaking = await Db.Stocktakings
            .AnyAsync(s => s.WarehouseId == id && s.IsDeleted != true, cancellationToken);
        if (hasStocktaking)
        {
            return Conflict(new { message = "این انبار دارای سابقه انبارگردانی است و قابل حذف نیست." });
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = DateTime.Now;
        entity.DeletedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "انبار با موفقیت حذف شد." });
    }

    public class SaveWarehouseRequest
    {
        [Required(ErrorMessage = "نام الزامی است.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Location { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public WarehouseType WarehouseType { get; set; } = WarehouseType.RawMaterials;

        public decimal? Capacity { get; set; }

        public int? CapacityMeaurmentId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
