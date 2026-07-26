using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Controllers.Transport;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HamgamCementWeb.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/fixed-asset-categories")]
[Authorize]
public class FixedAssetCategoryController : FinanceControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(FixedAssetCategory.Name),
        [4] = nameof(FixedAssetCategory.DefaultUsefulLifeMonths),
        [5] = nameof(FixedAssetCategory.IsActive),
    };

    public FixedAssetCategoryController(AppDbContext db) : base(db)
    {
    }

    [HttpPost("datatable")]
    [HasPermission("accounting.fixed-asset-categories.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.FixedAssetCategories
            .AsNoTracking()
            .Where(c => c.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(c =>
                c.Name.Contains(searchValue) ||
                (c.Code != null && c.Code.Contains(searchValue)) ||
                (c.Description != null && c.Description.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(FixedAssetCategory.CreatedAt))
            .Skip(start)
            .Take(length)
            .Select(c => new
            {
                fixedAssetCategoryId = c.FixedAssetCategoryID,
                name = c.Name,
                code = c.Code,
                description = c.Description,
                isSystem = c.IsSystem,
                assetAccountId = c.AssetAccountId,
                assetAccountName = c.AssetAccount != null ? c.AssetAccount.Name : null,
                defaultUsefulLifeMonths = c.DefaultUsefulLifeMonths,
                assetsCount = c.Assets.Count(a => a.IsDeleted != true),
                isActive = c.IsActive == true,
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
                r.fixedAssetCategoryId,
                r.name,
                r.code,
                r.description,
                r.isSystem,
                r.assetAccountId,
                r.assetAccountName,
                r.defaultUsefulLifeMonths,
                r.assetsCount,
                r.isActive,
            }),
        });
    }

    [HttpGet("list")]
    public async Task<IActionResult> List(CancellationToken cancellationToken = default)
    {
        var rows = await Db.FixedAssetCategories
            .AsNoTracking()
            .Where(c => c.IsDeleted != true && c.IsActive == true)
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                value = c.FixedAssetCategoryID,
                label = c.Name,
                defaultUsefulLifeMonths = c.DefaultUsefulLifeMonths,
            })
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost]
    [HasPermission("accounting.fixed-asset-categories.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveFixedAssetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.DefaultUsefulLifeMonths <= 0)
        {
            return BadRequest(new { message = "عمر مفید باید بزرگ‌تر از صفر باشد." });
        }

        var defaultAssetAccountId = request.AssetAccountId;
        if (defaultAssetAccountId is null or 0)
        {
            defaultAssetAccountId = await Db.Accounts
                .Where(a => a.SystemCode == AccountSystemCode.FixedAssetMachinery && a.IsDeleted != true)
                .Select(a => (int?)a.AccountID)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var accumId = request.AccumulatedDepreciationAccountId
            ?? await Db.Accounts
                .Where(a => a.SystemCode == AccountSystemCode.AccumulatedDepreciation && a.IsDeleted != true)
                .Select(a => (int?)a.AccountID)
                .FirstOrDefaultAsync(cancellationToken);

        var depExpId = request.DepreciationExpenseAccountId
            ?? await Db.Accounts
                .Where(a => a.SystemCode == AccountSystemCode.DepreciationExpense && a.IsDeleted != true)
                .Select(a => (int?)a.AccountID)
                .FirstOrDefaultAsync(cancellationToken);

        var category = new FixedAssetCategory
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            AssetAccountId = defaultAssetAccountId,
            AccumulatedDepreciationAccountId = accumId,
            DepreciationExpenseAccountId = depExpId,
            DefaultUsefulLifeMonths = request.DefaultUsefulLifeMonths,
            IsSystem = false,
            IsActive = request.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };

        Db.FixedAssetCategories.Add(category);
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "دسته‌بندی دارایی ثبت شد.", fixedAssetCategoryId = category.FixedAssetCategoryID });
    }

    [HttpPut("{id:int}")]
    [HasPermission("accounting.fixed-asset-categories.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveFixedAssetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var category = await Db.FixedAssetCategories
            .FirstOrDefaultAsync(c => c.FixedAssetCategoryID == id && c.IsDeleted != true, cancellationToken);
        if (category is null)
        {
            return NotFound(new { message = "دسته‌بندی یافت نشد." });
        }

        if (request.DefaultUsefulLifeMonths <= 0)
        {
            return BadRequest(new { message = "عمر مفید باید بزرگ‌تر از صفر باشد." });
        }

        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        category.AssetAccountId = request.AssetAccountId;
        category.AccumulatedDepreciationAccountId = request.AccumulatedDepreciationAccountId;
        category.DepreciationExpenseAccountId = request.DepreciationExpenseAccountId;
        category.DefaultUsefulLifeMonths = request.DefaultUsefulLifeMonths;
        category.IsActive = request.IsActive ?? category.IsActive;
        category.IsUpdated = true;
        category.UpdatedAt = DateTime.Now;
        category.UpdatedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "دسته‌بندی ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("accounting.fixed-asset-categories.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var category = await Db.FixedAssetCategories
            .FirstOrDefaultAsync(c => c.FixedAssetCategoryID == id && c.IsDeleted != true, cancellationToken);
        if (category is null)
        {
            return NotFound(new { message = "دسته‌بندی یافت نشد." });
        }

        if (category.IsSystem)
        {
            return Conflict(new { message = "دسته‌بندی سیستمی قابل حذف نیست." });
        }

        var hasAssets = await Db.FixedAssets
            .AnyAsync(a => a.FixedAssetCategoryId == id && a.IsDeleted != true, cancellationToken);
        if (hasAssets)
        {
            return Conflict(new { message = "این دسته دارای دارایی است و قابل حذف نیست." });
        }

        category.IsDeleted = true;
        category.IsActive = false;
        category.DeletedAt = DateTime.Now;
        category.DeletedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "دسته‌بندی حذف شد." });
    }

    public class SaveFixedAssetCategoryRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int? AssetAccountId { get; set; }
        public int? AccumulatedDepreciationAccountId { get; set; }
        public int? DepreciationExpenseAccountId { get; set; }

        [Range(1, 1200)]
        public int DefaultUsefulLifeMonths { get; set; } = 60;

        public bool? IsActive { get; set; }
    }
}
