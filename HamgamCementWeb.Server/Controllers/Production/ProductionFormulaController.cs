using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Controllers.Transport;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Production;

[ApiController]
[Route("api/production/formulas")]
[Authorize]
public class ProductionFormulaController : ControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(ProductionFormula.Name),
        [2] = nameof(ProductionFormula.ProductId),
        [3] = nameof(ProductionFormula.Mode),
    };

    private readonly AppDbContext _db;

    public ProductionFormulaController(AppDbContext db)
    {
        _db = db;
    }

    private int? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string ModeLabel(ProductionFormulaMode mode) => mode switch
    {
        ProductionFormulaMode.Fixed => "ثابت",
        ProductionFormulaMode.Variable => "متغیر",
        _ => mode.ToString(),
    };

    [HttpPost("datatable")]
    [HasPermission("production.formulas.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = _db.ProductionFormulas
            .AsNoTracking()
            .Where(f => f.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(f =>
                f.Name.Contains(searchValue) ||
                f.Product.Name.Contains(searchValue) ||
                (f.Notes != null && f.Notes.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(ProductionFormula.Name))
            .Skip(start)
            .Take(length)
            .Select(f => new
            {
                productionFormulaId = f.ProductionFormulaID,
                name = f.Name,
                productId = f.ProductId,
                productName = f.Product.Name,
                meaurmentId = f.MeaurmentId,
                meaurmentName = f.Meaurment.Name,
                baseQuantity = f.BaseQuantity,
                mode = (int)f.Mode,
                isDefault = f.IsDefault,
                materialLinesCount = f.MaterialLines.Count(x => x.IsDeleted != true),
                costLinesCount = f.CostLines.Count(x => x.IsDeleted != true),
                notes = f.Notes,
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
                r.productionFormulaId,
                r.name,
                r.productId,
                r.productName,
                r.meaurmentId,
                r.meaurmentName,
                r.baseQuantity,
                r.mode,
                modeLabel = ModeLabel((ProductionFormulaMode)r.mode),
                r.isDefault,
                r.materialLinesCount,
                r.costLinesCount,
                r.notes,
            }),
        });
    }

    // برای دراپ‌داون ثبت تولید — با دسترسی مشاهده تولید روزانه هم مجاز است
    [HttpGet("list")]
    [Authorize]
    public async Task<IActionResult> List(
        [FromQuery] int? productId,
        CancellationToken cancellationToken)
    {
        var query = _db.ProductionFormulas
            .AsNoTracking()
            .Where(f => f.IsDeleted != true && f.IsActive != false);

        if (productId is > 0)
        {
            query = query.Where(f => f.ProductId == productId);
        }

        var items = await query
            .OrderByDescending(f => f.IsDefault)
            .ThenBy(f => f.Name)
            .Select(f => new
            {
                value = f.ProductionFormulaID,
                label = f.IsDefault ? $"{f.Name} (پیش‌فرض)" : f.Name,
                productId = f.ProductId,
                productName = f.Product.Name,
                meaurmentId = f.MeaurmentId,
                baseQuantity = f.BaseQuantity,
                mode = (int)f.Mode,
                isDefault = f.IsDefault,
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [HasPermission("production.formulas.view")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var formula = await LoadFormulaDtoAsync(id, cancellationToken);
        if (formula is null)
        {
            return NotFound(new { message = "فرمول تولید یافت نشد." });
        }

        return Ok(formula);
    }

    // جزئیات فرمول برای پر کردن فرم ثبت تولید
    [HttpGet("{id:int}/for-production")]
    [HasPermission("production.daily.view")]
    public async Task<IActionResult> GetForProduction(int id, CancellationToken cancellationToken)
    {
        var formula = await LoadFormulaDtoAsync(id, cancellationToken);
        if (formula is null)
        {
            return NotFound(new { message = "فرمول تولید یافت نشد." });
        }

        return Ok(formula);
    }

    [HttpPost]
    [HasPermission("production.formulas.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveProductionFormulaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var error = await ValidateFormulaRequestAsync(request, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        if (request.IsDefault)
        {
            await ClearDefaultForProductAsync(request.ProductId, excludeId: null, userId, now, cancellationToken);
        }

        var formula = new ProductionFormula
        {
            Name = request.Name.Trim(),
            ProductId = request.ProductId,
            MeaurmentId = request.MeaurmentId,
            BaseQuantity = request.BaseQuantity,
            Mode = request.Mode,
            IsDefault = request.IsDefault,
            Notes = request.Notes?.Trim(),
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedBy = userId,
        };

        AddLines(formula, request, userId, now);
        _db.ProductionFormulas.Add(formula);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "فرمول تولید ایجاد شد.", productionFormulaId = formula.ProductionFormulaID });
    }

    [HttpPut("{id:int}")]
    [HasPermission("production.formulas.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveProductionFormulaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var formula = await _db.ProductionFormulas
            .Include(f => f.MaterialLines.Where(x => x.IsDeleted != true))
            .Include(f => f.CostLines.Where(x => x.IsDeleted != true))
            .FirstOrDefaultAsync(f => f.ProductionFormulaID == id && f.IsDeleted != true, cancellationToken);

        if (formula is null)
        {
            return NotFound(new { message = "فرمول تولید یافت نشد." });
        }

        var error = await ValidateFormulaRequestAsync(request, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        if (request.IsDefault)
        {
            await ClearDefaultForProductAsync(request.ProductId, excludeId: id, userId, now, cancellationToken);
        }

        formula.Name = request.Name.Trim();
        formula.ProductId = request.ProductId;
        formula.MeaurmentId = request.MeaurmentId;
        formula.BaseQuantity = request.BaseQuantity;
        formula.Mode = request.Mode;
        formula.IsDefault = request.IsDefault;
        formula.Notes = request.Notes?.Trim();
        formula.IsUpdated = true;
        formula.UpdatedAt = now;
        formula.UpdatedBy = userId;

        foreach (var line in formula.MaterialLines.ToList())
        {
            line.IsDeleted = true;
            line.DeletedAt = now;
            line.DeletedBy = userId;
        }

        foreach (var line in formula.CostLines.ToList())
        {
            line.IsDeleted = true;
            line.DeletedAt = now;
            line.DeletedBy = userId;
        }

        AddLines(formula, request, userId, now);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "فرمول تولید ویرایش شد." });
    }

    [HttpPost("{id:int}/set-default")]
    [HasPermission("production.formulas.edit")]
    public async Task<IActionResult> SetDefault(int id, CancellationToken cancellationToken)
    {
        var formula = await _db.ProductionFormulas
            .FirstOrDefaultAsync(f => f.ProductionFormulaID == id && f.IsDeleted != true, cancellationToken);

        if (formula is null)
        {
            return NotFound(new { message = "فرمول تولید یافت نشد." });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        await ClearDefaultForProductAsync(formula.ProductId, excludeId: id, userId, now, cancellationToken);
        formula.IsDefault = true;
        formula.IsUpdated = true;
        formula.UpdatedAt = now;
        formula.UpdatedBy = userId;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "فرمول به‌عنوان پیش‌فرض تنظیم شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("production.formulas.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var formula = await _db.ProductionFormulas
            .Include(f => f.MaterialLines)
            .Include(f => f.CostLines)
            .FirstOrDefaultAsync(f => f.ProductionFormulaID == id && f.IsDeleted != true, cancellationToken);

        if (formula is null)
        {
            return NotFound(new { message = "فرمول تولید یافت نشد." });
        }

        var used = await _db.ProductionBatches
            .AnyAsync(b => b.ProductionFormulaId == id && b.IsDeleted != true, cancellationToken);
        if (used)
        {
            return BadRequest(new { message = "این فرمول در سند تولید استفاده شده و قابل حذف نیست." });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        formula.IsDeleted = true;
        formula.IsActive = false;
        formula.IsDefault = false;
        formula.DeletedAt = now;
        formula.DeletedBy = userId;

        foreach (var line in formula.MaterialLines)
        {
            line.IsDeleted = true;
            line.DeletedAt = now;
            line.DeletedBy = userId;
        }

        foreach (var line in formula.CostLines)
        {
            line.IsDeleted = true;
            line.DeletedAt = now;
            line.DeletedBy = userId;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "فرمول تولید حذف شد." });
    }

    private async Task<object?> LoadFormulaDtoAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.ProductionFormulas
            .AsNoTracking()
            .Where(f => f.ProductionFormulaID == id && f.IsDeleted != true)
            .Select(f => new
            {
                productionFormulaId = f.ProductionFormulaID,
                name = f.Name,
                productId = f.ProductId,
                productName = f.Product.Name,
                meaurmentId = f.MeaurmentId,
                meaurmentName = f.Meaurment.Name,
                baseQuantity = f.BaseQuantity,
                mode = (int)f.Mode,
                modeLabel = f.Mode == ProductionFormulaMode.Fixed ? "ثابت" : "متغیر",
                isDefault = f.IsDefault,
                notes = f.Notes,
                materialLines = f.MaterialLines
                    .Where(x => x.IsDeleted != true)
                    .Select(x => new
                    {
                        productionFormulaMaterialLineId = x.ProductionFormulaMaterialLineID,
                        productId = x.ProductId,
                        productName = x.Product.Name,
                        meaurmentId = x.MeaurmentId,
                        meaurmentName = x.Meaurment.Name,
                        quantity = x.Quantity,
                        defaultWarehouseId = x.DefaultWarehouseId,
                        defaultWarehouseName = x.DefaultWarehouse != null ? x.DefaultWarehouse.Name : null,
                    })
                    .ToList(),
                costLines = f.CostLines
                    .Where(x => x.IsDeleted != true)
                    .Select(x => new
                    {
                        productionFormulaCostLineId = x.ProductionFormulaCostLineID,
                        costType = (int)x.CostType,
                        description = x.Description,
                        amountMode = (int)x.AmountMode,
                        amount = x.Amount,
                        accountId = x.AccountId,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task ClearDefaultForProductAsync(
        int productId,
        int? excludeId,
        int? userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var others = await _db.ProductionFormulas
            .Where(f =>
                f.ProductId == productId &&
                f.IsDefault &&
                f.IsDeleted != true &&
                (excludeId == null || f.ProductionFormulaID != excludeId))
            .ToListAsync(cancellationToken);

        foreach (var other in others)
        {
            other.IsDefault = false;
            other.IsUpdated = true;
            other.UpdatedAt = now;
            other.UpdatedBy = userId;
        }
    }

    private async Task<string?> ValidateFormulaRequestAsync(
        SaveProductionFormulaRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "نام فرمول الزامی است.";
        }

        if (request.BaseQuantity <= 0)
        {
            return "مقدار پایه باید بزرگ‌تر از صفر باشد.";
        }

        if (request.MaterialLines is null || request.MaterialLines.Count == 0)
        {
            return "حداقل یک ردیف مواد وارد کنید.";
        }

        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductID == request.ProductId && p.IsDeleted != true, cancellationToken);
        if (product is null)
        {
            return "محصول خروجی یافت نشد.";
        }

        var productUnitOk = await _db.ProductMeaurments.AnyAsync(
            pm => pm.ProductId == request.ProductId &&
                  pm.MeaurmentId == request.MeaurmentId &&
                  pm.IsDeleted != true,
            cancellationToken);
        if (!productUnitOk)
        {
            return $"واحد انتخاب‌شده برای محصول «{product.Name}» مجاز نیست.";
        }

        foreach (var line in request.MaterialLines)
        {
            if (line.Quantity <= 0)
            {
                return "مقدار مواد باید بزرگ‌تر از صفر باشد.";
            }

            var mat = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductID == line.ProductId && p.IsDeleted != true, cancellationToken);
            if (mat is null)
            {
                return "یکی از مواد فرمول یافت نشد.";
            }

            var unitOk = await _db.ProductMeaurments.AnyAsync(
                pm => pm.ProductId == line.ProductId &&
                      pm.MeaurmentId == line.MeaurmentId &&
                      pm.IsDeleted != true,
                cancellationToken);
            if (!unitOk)
            {
                return $"واحد انتخاب‌شده برای ماده «{mat.Name}» مجاز نیست.";
            }

            if (line.DefaultWarehouseId is > 0)
            {
                var wh = await _db.Warehouses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.WarehouseID == line.DefaultWarehouseId && w.IsDeleted != true, cancellationToken);
                if (wh is null)
                {
                    return "انبار پیش‌فرض یکی از مواد یافت نشد.";
                }

                if (wh.WarehouseType is not (WarehouseType.RawMaterials or WarehouseType.SemiFinished))
                {
                    return $"انبار «{wh.Name}» برای مواد فرمول مجاز نیست.";
                }
            }
        }

        foreach (var line in request.CostLines ?? [])
        {
            if (line.Amount < 0)
            {
                return "مبلغ هزینه نمی‌تواند منفی باشد.";
            }

            if (line.AccountId is > 0)
            {
                var accountOk = await _db.Accounts.AnyAsync(
                    a => a.AccountID == line.AccountId && a.IsDeleted != true && a.IsPostable,
                    cancellationToken);
                if (!accountOk)
                {
                    return "یکی از حساب‌های هزینه قابل‌ثبت نیست.";
                }
            }
        }

        return null;
    }

    private static void AddLines(
        ProductionFormula formula,
        SaveProductionFormulaRequest request,
        int? userId,
        DateTime now)
    {
        foreach (var line in request.MaterialLines)
        {
            formula.MaterialLines.Add(new ProductionFormulaMaterialLine
            {
                ProductId = line.ProductId,
                MeaurmentId = line.MeaurmentId,
                Quantity = line.Quantity,
                DefaultWarehouseId = line.DefaultWarehouseId is > 0 ? line.DefaultWarehouseId : null,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            });
        }

        foreach (var line in request.CostLines ?? [])
        {
            formula.CostLines.Add(new ProductionFormulaCostLine
            {
                CostType = line.CostType,
                Description = line.Description?.Trim(),
                AmountMode = line.AmountMode,
                Amount = line.Amount,
                AccountId = line.AccountId is > 0 ? line.AccountId : null,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            });
        }
    }

    public class SaveProductionFormulaRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int MeaurmentId { get; set; }

        [Range(0.000001, double.MaxValue)]
        public decimal BaseQuantity { get; set; } = 1;

        public ProductionFormulaMode Mode { get; set; } = ProductionFormulaMode.Fixed;

        public bool IsDefault { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public List<SaveFormulaMaterialLineRequest> MaterialLines { get; set; } = [];

        public List<SaveFormulaCostLineRequest> CostLines { get; set; } = [];
    }

    public class SaveFormulaMaterialLineRequest
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int MeaurmentId { get; set; }

        [Range(0.000001, double.MaxValue)]
        public decimal Quantity { get; set; }

        public int? DefaultWarehouseId { get; set; }
    }

    public class SaveFormulaCostLineRequest
    {
        public ProductionCostType CostType { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }

        public ProductionCostAmountMode AmountMode { get; set; } = ProductionCostAmountMode.PerBase;

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        public int? AccountId { get; set; }
    }
}
