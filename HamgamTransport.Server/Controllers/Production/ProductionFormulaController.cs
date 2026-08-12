using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamTransport.Server.Authorization;
using HamgamTransport.Server.Controllers.Common;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Production;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Controllers.Production;

[ApiController]
[Route("api/production/formulas")]
[Authorize]
public class ProductionFormulaController : ControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = "Name",
        [2] = "ProductName",
        [3] = "BaseQuantity",
        [4] = "Mode",
        [5] = "IsDefault",
    };

    private readonly AppDbContext _db;
    private readonly IProductionFormulaReadService _formulaRead;

    public ProductionFormulaController(AppDbContext db, IProductionFormulaReadService formulaRead)
    {
        _db = db;
        _formulaRead = formulaRead;
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
        var order = request.Order?.FirstOrDefault();
        var orderColumn = order is not null && OrderColumns.TryGetValue(order.Column, out var col)
            ? col
            : "Name";
        var ascending = !string.Equals(order?.Dir, "desc", StringComparison.OrdinalIgnoreCase);

        var (recordsTotal, recordsFiltered, rows) = await _formulaRead.GetDataTableAsync(
            start,
            length,
            request.Search?.Value,
            orderColumn,
            ascending,
            cancellationToken);

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select((r, i) => new
            {
                rowNumber = start + i + 1,
                productionFormulaId = r.ProductionFormulaId,
                name = r.Name,
                productId = r.ProductId,
                productName = r.ProductName,
                meaurmentId = r.MeaurmentId,
                meaurmentName = r.MeaurmentName,
                baseQuantity = r.BaseQuantity,
                mode = r.Mode,
                modeLabel = ModeLabel((ProductionFormulaMode)r.Mode),
                isDefault = r.IsDefault,
                materialLinesCount = r.MaterialLinesCount,
                costLinesCount = r.CostLinesCount,
                notes = r.Notes,
            }),
        });
    }

    // پیشنهاد هزینه‌های سیستمی (حقوق بخش تولید و سایر پرسنل)
    [HttpGet("system-cost-hints")]
    [HasPermission("production.formulas.view")]
    public async Task<IActionResult> SystemCostHints(CancellationToken cancellationToken)
    {
        var hints = await _formulaRead.GetSystemCostHintsAsync(cancellationToken);
        return Ok(hints);
    }

    // برای دراپ‌داون ثبت تولید — با دسترسی مشاهده تولید روزانه هم مجاز است
    [HttpGet("list")]
    [Authorize]
    public async Task<IActionResult> List(
        [FromQuery] int? productId,
        CancellationToken cancellationToken)
    {
        var items = await _formulaRead.GetListAsync(productId, cancellationToken);
        return Ok(items.Select(f => new
        {
            value = f.Value,
            label = f.Label,
            productId = f.ProductId,
            productName = f.ProductName,
            meaurmentId = f.MeaurmentId,
            baseQuantity = f.BaseQuantity,
            mode = f.Mode,
            isDefault = f.IsDefault,
        }));
    }

    [HttpGet("{id:int}")]
    [HasPermission("production.formulas.view")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var formula = await _formulaRead.GetByIdAsync(id, cancellationToken);
        if (formula is null)
        {
            return NotFound(new { message = "فرمول تولید یافت نشد." });
        }

        return Ok(ToFormulaResponse(formula));
    }

    // جزئیات فرمول برای پر کردن فرم ثبت تولید
    [HttpGet("{id:int}/for-production")]
    [HasPermission("production.daily.view")]
    public async Task<IActionResult> GetForProduction(int id, CancellationToken cancellationToken)
    {
        var formula = await _formulaRead.GetByIdAsync(id, cancellationToken);
        if (formula is null)
        {
            return NotFound(new { message = "فرمول تولید یافت نشد." });
        }

        return Ok(ToFormulaResponse(formula));
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

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        if (request.IsDefault)
        {
            // پاک‌سازی پیش‌فرض‌ها باید قبل از Insert ذخیره شود تا ایندکس یکتا نقض نشود
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
        await transaction.CommitAsync(cancellationToken);

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

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        if (request.IsDefault)
        {
            // ابتدا پیش‌فرض‌های دیگر را در DB پاک کن، بعد این ردیف را پیش‌فرض کن
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
        await transaction.CommitAsync(cancellationToken);

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

        // ایندکس یکتای فیلترشدهٔ ProductId (IsDefault=1) با دو UPDATE همزمان نقض می‌شود
        // اگر ردیف جدید قبل از پاک‌شدن پیش‌فرض قبلی نوشته شود — بنابراین دو مرحله جدا.
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        await ClearDefaultForProductAsync(formula.ProductId, excludeId: id, userId, now, cancellationToken);

        formula.IsDefault = true;
        formula.IsUpdated = true;
        formula.UpdatedAt = now;
        formula.UpdatedBy = userId;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

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

    private static object ToFormulaResponse(ProductionFormulaDetailDto formula) => new
    {
        productionFormulaId = formula.ProductionFormulaId,
        name = formula.Name,
        productId = formula.ProductId,
        productName = formula.ProductName,
        meaurmentId = formula.MeaurmentId,
        meaurmentName = formula.MeaurmentName,
        baseQuantity = formula.BaseQuantity,
        mode = formula.Mode,
        modeLabel = formula.ModeLabel,
        isDefault = formula.IsDefault,
        notes = formula.Notes,
        materialLines = formula.MaterialLines.Select(x => new
        {
            productionFormulaMaterialLineId = x.ProductionFormulaMaterialLineId,
            productId = x.ProductId,
            productName = x.ProductName,
            meaurmentId = x.MeaurmentId,
            meaurmentName = x.MeaurmentName,
            quantity = x.Quantity,
            defaultWarehouseId = x.DefaultWarehouseId,
            defaultWarehouseName = x.DefaultWarehouseName,
        }),
        costLines = formula.CostLines.Select(x => new
        {
            productionFormulaCostLineId = x.ProductionFormulaCostLineId,
            costType = x.CostType,
            productionCostCategoryId = x.ProductionCostCategoryId,
            costCategoryName = x.CostCategoryName,
            description = x.Description,
            amountMode = x.AmountMode,
            amount = x.Amount,
            accountId = x.AccountId,
        }),
    };

    /// <summary>
    /// پیش‌فرض‌های فعال همان محصول را بلافاصله در دیتابیس پاک می‌کند (ExecuteUpdate)
    /// تا قبل از ست‌کردن پیش‌فرض جدید، ایندکس یکتای فیلترشده نقض نشود.
    /// </summary>
    private async Task ClearDefaultForProductAsync(
        int productId,
        int? excludeId,
        int? userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await _db.ProductionFormulas
            .Where(f =>
                f.ProductId == productId &&
                f.IsDefault &&
                f.IsDeleted != true &&
                (excludeId == null || f.ProductionFormulaID != excludeId))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(f => f.IsDefault, false)
                    .SetProperty(f => f.IsUpdated, true)
                    .SetProperty(f => f.UpdatedAt, now)
                    .SetProperty(f => f.UpdatedBy, userId),
                cancellationToken);
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
                ProductionCostCategoryId = line.ProductionCostCategoryId is > 0
                    ? line.ProductionCostCategoryId
                    : null,
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

        public int? ProductionCostCategoryId { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }

        public ProductionCostAmountMode AmountMode { get; set; } = ProductionCostAmountMode.PerBase;

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        public int? AccountId { get; set; }
    }
}
