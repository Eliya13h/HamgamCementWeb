using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Controllers.Transport;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Inventory;
using HamgamCementWeb.Server.Services;
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
        [6] = nameof(Warehouse.IsActive),
    };

    private readonly IMeaurmentConversionService _conversion;

    public WarehouseController(AppDbContext db, IMeaurmentConversionService conversion) : base(db)
    {
        _conversion = conversion;
    }

    [HttpPost("datatable")]
    [HasPermission("inventory.warehouses.view")]
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
                isActive = w.IsActive == true,
            })
            .ToListAsync(cancellationToken);

        var fillByWarehouse = await ComputeFillLevelsAsync(
            rows.Select(r => (
                r.warehouseId,
                r.capacity,
                r.capacityMeaurmentId,
                r.capacityMeaurmentName)),
            cancellationToken);

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select((r, i) =>
            {
                fillByWarehouse.TryGetValue(r.warehouseId, out var fill);
                return new
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
                    usedQuantity = fill.UsedQuantity,
                    fillPercent = fill.FillPercent,
                    fillText = BuildFillText(fill.UsedQuantity, r.capacity, r.capacityMeaurmentName, fill.FillPercent),
                    r.isActive,
                };
            }),
        });
    }

    // چرا بدون HasPermission: دراپ‌داون انبار در فاکتور خرید/فروش، تولید و انبارگردانی
    // استفاده می‌شود؛ فقط احراز هویت لازم است تا صفحات وابسته قفل نشوند.
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

    // سطح پرشدن انبارها برای داشبورد — فقط احراز هویت (مشابه list)
    [HttpGet("fill-levels")]
    public async Task<IActionResult> FillLevels(CancellationToken cancellationToken)
    {
        var warehouses = await Db.Warehouses
            .AsNoTracking()
            .Where(w => w.IsDeleted != true && w.IsActive == true)
            .OrderBy(w => w.Name)
            .Select(w => new
            {
                w.WarehouseID,
                w.Name,
                w.WarehouseType,
                w.Location,
                w.Capacity,
                w.CapacityMeaurmentId,
                capacityMeaurmentName = w.CapacityMeaurment != null ? w.CapacityMeaurment.Name : null,
            })
            .ToListAsync(cancellationToken);

        var fillByWarehouse = await ComputeFillLevelsAsync(
            warehouses.Select(w => (
                w.WarehouseID,
                w.Capacity,
                w.CapacityMeaurmentId,
                w.capacityMeaurmentName)),
            cancellationToken);

        var result = warehouses.Select(warehouse =>
        {
            fillByWarehouse.TryGetValue(warehouse.WarehouseID, out var fill);
            return new
            {
                warehouseId = warehouse.WarehouseID,
                name = warehouse.Name,
                warehouseType = (int)warehouse.WarehouseType,
                warehouseTypeLabel = GetWarehouseTypeLabel(warehouse.WarehouseType),
                location = warehouse.Location,
                capacity = warehouse.Capacity,
                capacityUnit = warehouse.capacityMeaurmentName,
                usedQuantity = fill.UsedQuantity,
                fillPercent = fill.FillPercent,
            };
        });

        return Ok(result);
    }

    private async Task<Dictionary<int, WarehouseFillLevel>> ComputeFillLevelsAsync(
        IEnumerable<(int WarehouseId, decimal? Capacity, int? CapacityMeaurmentId, string? CapacityUnit)> warehouses,
        CancellationToken cancellationToken)
    {
        var list = warehouses.ToList();
        var warehouseIds = list.Select(w => w.WarehouseId).Distinct().ToList();
        var result = warehouseIds.ToDictionary(
            id => id,
            _ => new WarehouseFillLevel(null, null));

        if (warehouseIds.Count == 0)
        {
            return result;
        }

        var stocks = await Db.InventoryStocks
            .AsNoTracking()
            .Where(s =>
                warehouseIds.Contains(s.WarehouseId) &&
                s.IsDeleted != true &&
                s.QuantityInBase > 0)
            .Select(s => new
            {
                s.WarehouseId,
                s.QuantityInBase,
                s.Product.BaseMeaurmentId,
            })
            .ToListAsync(cancellationToken);

        var stocksByWarehouse = stocks.GroupBy(s => s.WarehouseId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var warehouse in list)
        {
            if (warehouse.Capacity is not > 0 || warehouse.CapacityMeaurmentId is not int capacityUnitId)
            {
                continue;
            }

            try
            {
                var capacityUnitEntity = await _conversion.GetMeaurmentAsync(capacityUnitId, cancellationToken);
                var capacityRootBaseId = _conversion.GetRootBaseMeaurmentId(capacityUnitEntity);
                var capacityInBase = _conversion.ToBaseQuantity(warehouse.Capacity.Value, capacityUnitEntity);

                var usedInBase = 0m;
                if (stocksByWarehouse.TryGetValue(warehouse.WarehouseId, out var warehouseStocks))
                {
                    usedInBase = warehouseStocks
                        .Where(s => s.BaseMeaurmentId == capacityRootBaseId)
                        .Sum(s => s.QuantityInBase);
                }

                var usedQuantity = _conversion.FromBaseQuantity(usedInBase, capacityUnitEntity);
                var fillPercent = capacityInBase > 0
                    ? Math.Round(Math.Min(100m, usedInBase / capacityInBase * 100m), 1)
                    : 0m;

                result[warehouse.WarehouseId] = new WarehouseFillLevel(usedQuantity, fillPercent);
            }
            catch (InvalidOperationException)
            {
                // واحد ظرفیت نامعتبر — درصد قابل محاسبه نیست
            }
        }

        return result;
    }

    private static string? BuildFillText(
        decimal? usedQuantity,
        decimal? capacity,
        string? capacityUnit,
        decimal? fillPercent)
    {
        if (fillPercent is null || capacity is not > 0)
        {
            return null;
        }

        var usedText = (usedQuantity ?? 0m).ToString("N2");
        var capacityText = capacity.Value.ToString("N2");
        var unit = string.IsNullOrWhiteSpace(capacityUnit) ? "" : $" {capacityUnit.Trim()}";
        return $"{usedText} / {capacityText}{unit} ({fillPercent:0.#}٪)";
    }

    private readonly record struct WarehouseFillLevel(decimal? UsedQuantity, decimal? FillPercent);

    [HttpPost]
    [HasPermission("inventory.warehouses.create")]
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
    [HasPermission("inventory.warehouses.edit")]
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
    [HasPermission("inventory.warehouses.delete")]
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

        var hasTransfer = await Db.WarehouseTransfers
            .AnyAsync(t =>
                t.IsDeleted != true &&
                (t.FromWarehouseId == id || t.ToWarehouseId == id), cancellationToken);
        if (hasTransfer)
        {
            return Conflict(new { message = "این انبار در اسناد انتقال استفاده شده و قابل حذف نیست." });
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
