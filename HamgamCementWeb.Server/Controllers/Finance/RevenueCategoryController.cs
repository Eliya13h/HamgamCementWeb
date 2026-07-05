using System.ComponentModel.DataAnnotations;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Controllers.Transport;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/revenue-categories")]
[Authorize]
public class RevenueCategoryController : FinanceControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(RevenueCategory.Name),
        [4] = nameof(RevenueCategory.IsActive),
    };

    public RevenueCategoryController(AppDbContext db) : base(db)
    {
    }

    [HttpPost("datatable")]
    [HasPermission("accounting.revenue-categories.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.RevenueCategories
            .AsNoTracking()
            .Where(c => c.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(c =>
                c.Name.Contains(searchValue) ||
                (c.Description != null && c.Description.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(RevenueCategory.CreatedAt))
            .Skip(start)
            .Take(length)
            .Select(c => new
            {
                revenueCategoryId = c.RevenueCategoryID,
                name = c.Name,
                description = c.Description,
                isSystem = c.IsSystem,
                revenuesCount = c.Revenues.Count(r => r.IsDeleted != true),
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
                r.revenueCategoryId,
                r.name,
                r.description,
                r.isSystem,
                r.revenuesCount,
                r.isActive,
            }),
        });
    }

    // چرا بدون HasPermission: دراپ‌داون دسته‌بندی در فرم ثبت عاید (صفحه‌ی عواید) هم
    // استفاده می‌شود؛ فقط احراز هویت لازم است تا کاربرانِ عواید قفل نشوند.
    [HttpGet("list")]
    public async Task<IActionResult> List(
        [FromQuery] bool forEntry = false,
        CancellationToken cancellationToken = default)
    {
        var query = Db.RevenueCategories
            .AsNoTracking()
            .Where(c => c.IsDeleted != true && c.IsActive == true);

        if (forEntry)
        {
            query = query.Where(c =>
                !c.IsSystem || c.Code == FinanceCategoryCode.MiscellaneousRevenue);
        }

        var items = await query
            .OrderBy(c => c.IsSystem ? 1 : 0)
            .ThenBy(c => c.Name)
            .Select(c => new { value = c.RevenueCategoryID, label = c.Name, isSystem = c.IsSystem })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost]
    [HasPermission("accounting.revenue-categories.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveRevenueCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var name = request.Name.Trim();
        var exists = await Db.RevenueCategories
            .AnyAsync(c => c.IsDeleted != true && c.Name == name, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "دسته‌بندی با این نام قبلاً ثبت شده است." });
        }

        Db.RevenueCategories.Add(new RevenueCategory
        {
            Name = name,
            Description = request.Description?.Trim(),
            IsSystem = false,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        });

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "دسته‌بندی عاید با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
    [HasPermission("accounting.revenue-categories.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveRevenueCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var category = await Db.RevenueCategories
            .FirstOrDefaultAsync(c => c.RevenueCategoryID == id && c.IsDeleted != true, cancellationToken);
        if (category is null)
        {
            return NotFound(new { message = "دسته‌بندی یافت نشد." });
        }

        if (category.IsSystem && category.Code == FinanceCategoryCode.ProductSale)
        {
            return Conflict(new { message = "دسته‌بندی سیستمی فروش محصولات قابل ویرایش نیست." });
        }

        var name = request.Name.Trim();
        var exists = await Db.RevenueCategories.AnyAsync(
            c => c.IsDeleted != true && c.Name == name && c.RevenueCategoryID != id,
            cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "دسته‌بندی با این نام قبلاً ثبت شده است." });
        }

        category.Name = name;
        category.Description = request.Description?.Trim();
        category.IsActive = request.IsActive;
        category.IsUpdated = true;
        category.UpdatedAt = DateTime.Now;
        category.UpdatedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "دسته‌بندی عاید با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("accounting.revenue-categories.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var category = await Db.RevenueCategories
            .FirstOrDefaultAsync(c => c.RevenueCategoryID == id && c.IsDeleted != true, cancellationToken);
        if (category is null)
        {
            return NotFound(new { message = "دسته‌بندی یافت نشد." });
        }

        if (category.IsSystem)
        {
            return Conflict(new { message = "دسته‌بندی‌های سیستمی قابل حذف نیستند." });
        }

        var inUse = await Db.Revenues
            .AnyAsync(r => r.RevenueCategoryId == id && r.IsDeleted != true, cancellationToken);
        if (inUse)
        {
            return Conflict(new { message = "این دسته‌بندی در عواید ثبت‌شده استفاده شده و قابل حذف نیست." });
        }

        category.IsDeleted = true;
        category.IsActive = false;
        category.DeletedAt = DateTime.Now;
        category.DeletedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "دسته‌بندی عاید با موفقیت حذف شد." });
    }

    public class SaveRevenueCategoryRequest
    {
        [Required(ErrorMessage = "نام دسته‌بندی الزامی است.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
