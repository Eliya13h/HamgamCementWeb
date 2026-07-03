using System.ComponentModel.DataAnnotations;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Transport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Transport;

[ApiController]
[Route("api/transport/expense-categories")]
[Authorize]
public class ExpensesCategoryController : TransportControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(ExpensesCategory.Name),
        [4] = nameof(ExpensesCategory.IsActive),
    };

    public ExpensesCategoryController(AppDbContext db) : base(db)
    {
    }

    [HttpPost("datatable")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.ExpensesCategories
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
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(ExpensesCategory.CreatedAt))
            .Skip(start)
            .Take(length)
            .Select(c => new
            {
                expensesCategoryId = c.ExpensesCategoryID,
                name = c.Name,
                description = c.Description,
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
                r.expensesCategoryId,
                r.name,
                r.description,
                r.expensesCount,
                r.isActive,
            }),
        });
    }

    // لیست دسته‌بندی‌ها برای دراپ‌داون‌ها
    [HttpGet("list")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await Db.ExpensesCategories
            .AsNoTracking()
            .Where(c => c.IsDeleted != true && c.IsActive == true)
            .OrderBy(c => c.Name)
            .Select(c => new { value = c.ExpensesCategoryID, label = c.Name })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveExpensesCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var name = request.Name.Trim();
        var exists = await Db.ExpensesCategories
            .AnyAsync(c => c.IsDeleted != true && c.Name == name, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "دسته‌بندی با این نام قبلاً ثبت شده است." });
        }

        Db.ExpensesCategories.Add(new ExpensesCategory
        {
            Name = name,
            Description = request.Description?.Trim(),
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        });

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "دسته‌بندی مصارف با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveExpensesCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var category = await Db.ExpensesCategories
            .FirstOrDefaultAsync(c => c.ExpensesCategoryID == id && c.IsDeleted != true, cancellationToken);
        if (category is null)
        {
            return NotFound(new { message = "دسته‌بندی یافت نشد." });
        }

        var name = request.Name.Trim();
        var exists = await Db.ExpensesCategories.AnyAsync(
            c => c.IsDeleted != true && c.Name == name && c.ExpensesCategoryID != id,
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

        return Ok(new { message = "دسته‌بندی مصارف با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var category = await Db.ExpensesCategories
            .FirstOrDefaultAsync(c => c.ExpensesCategoryID == id && c.IsDeleted != true, cancellationToken);
        if (category is null)
        {
            return NotFound(new { message = "دسته‌بندی یافت نشد." });
        }

        var inUse = await Db.TransportExpenses
            .AnyAsync(e => e.ExpensesCategoryId == id && e.IsDeleted != true, cancellationToken);
        if (inUse)
        {
            return Conflict(new { message = "این دسته‌بندی در مصارف ثبت‌شده استفاده شده و قابل حذف نیست." });
        }

        category.IsDeleted = true;
        category.IsActive = false;
        category.DeletedAt = DateTime.Now;
        category.DeletedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "دسته‌بندی مصارف با موفقیت حذف شد." });
    }

    public class SaveExpensesCategoryRequest
    {
        [Required(ErrorMessage = "نام دسته‌بندی الزامی است.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
