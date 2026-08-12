using System.ComponentModel.DataAnnotations;
using HamgamTransport.Server.Authorization;
using HamgamTransport.Server.Controllers.Common;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/expense-categories")]
[Authorize]
public class ExpenseCategoryController : FinanceControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(ExpenseCategory.Name),
        [4] = nameof(ExpenseCategory.IsActive),
    };

    public ExpenseCategoryController(AppDbContext db) : base(db)
    {
    }

    [HttpPost("datatable")]
    [HasPermission("accounting.expense-categories.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.ExpenseCategories
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
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(ExpenseCategory.CreatedAt))
            .Skip(start)
            .Take(length)
            .Select(c => new
            {
                expenseCategoryId = c.ExpenseCategoryID,
                name = c.Name,
                description = c.Description,
                isSystem = c.IsSystem,
                expensesCount = c.Expenses.Count(e => e.IsDeleted != true),
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
                r.expenseCategoryId,
                r.name,
                r.description,
                r.isSystem,
                r.expensesCount,
                r.isActive,
            }),
        });
    }

    // لیست دسته‌بندی‌ها برای دراپ‌داون ثبت مصرف متفرقه
    // چرا بدون HasPermission: این endpoint سبک در فرم ثبت مصرف (صفحه‌ی مصارف) هم
    // استفاده می‌شود؛ محدود کردن آن به دسترسیِ صفحه‌ی دسته‌بندی، کاربرانِ دارای فقط
    // دسترسی مصارف را قفل می‌کند. پس فقط احراز هویت کافی است.
    [HttpGet("list")]
    public async Task<IActionResult> List(
        [FromQuery] bool forEntry = false,
        CancellationToken cancellationToken = default)
    {
        var query = Db.ExpenseCategories
            .AsNoTracking()
            .Where(c => c.IsDeleted != true && c.IsActive == true);

        if (forEntry)
        {
            query = query.Where(c =>
                !c.IsSystem || c.Code == FinanceCategoryCode.MiscellaneousExpense);
        }

        var items = await query
            .OrderBy(c => c.IsSystem ? 1 : 0)
            .ThenBy(c => c.Name)
            .Select(c => new { value = c.ExpenseCategoryID, label = c.Name, isSystem = c.IsSystem })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost]
    [HasPermission("accounting.expense-categories.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveExpenseCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var name = request.Name.Trim();
        var exists = await Db.ExpenseCategories
            .AnyAsync(c => c.IsDeleted != true && c.Name == name, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "دسته‌بندی با این نام قبلاً ثبت شده است." });
        }

        Db.ExpenseCategories.Add(new ExpenseCategory
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

        return Ok(new { message = "دسته‌بندی مصرف با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
    [HasPermission("accounting.expense-categories.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveExpenseCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var category = await Db.ExpenseCategories
            .FirstOrDefaultAsync(c => c.ExpenseCategoryID == id && c.IsDeleted != true, cancellationToken);
        if (category is null)
        {
            return NotFound(new { message = "دسته‌بندی یافت نشد." });
        }

        if (category.IsSystem && category.Code == FinanceCategoryCode.ProductPurchase)
        {
            return Conflict(new { message = "دسته‌بندی سیستمی خرید محصولات قابل ویرایش نیست." });
        }

        var name = request.Name.Trim();
        var exists = await Db.ExpenseCategories.AnyAsync(
            c => c.IsDeleted != true && c.Name == name && c.ExpenseCategoryID != id,
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

        return Ok(new { message = "دسته‌بندی مصرف با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("accounting.expense-categories.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var category = await Db.ExpenseCategories
            .FirstOrDefaultAsync(c => c.ExpenseCategoryID == id && c.IsDeleted != true, cancellationToken);
        if (category is null)
        {
            return NotFound(new { message = "دسته‌بندی یافت نشد." });
        }

        if (category.IsSystem)
        {
            return Conflict(new { message = "دسته‌بندی‌های سیستمی قابل حذف نیستند." });
        }

        var inUse = await Db.Expenses
            .AnyAsync(e => e.ExpenseCategoryId == id && e.IsDeleted != true, cancellationToken);
        if (inUse)
        {
            return Conflict(new { message = "این دسته‌بندی در مصارف ثبت‌شده استفاده شده و قابل حذف نیست." });
        }

        category.IsDeleted = true;
        category.IsActive = false;
        category.DeletedAt = DateTime.Now;
        category.DeletedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "دسته‌بندی مصرف با موفقیت حذف شد." });
    }

    public class SaveExpenseCategoryRequest
    {
        [Required(ErrorMessage = "نام دسته‌بندی الزامی است.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
