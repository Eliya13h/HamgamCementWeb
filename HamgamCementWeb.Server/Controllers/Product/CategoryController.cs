using System.ComponentModel.DataAnnotations;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Controllers.Common;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Product;

[ApiController]
[Route("api/products/categories")]
[Authorize]
public class CategoryController : ProductControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(Category.Name),
        [2] = nameof(Category.Description),
        [3] = "ProductsCount",
        [4] = nameof(Category.IsActive),
    };

    public CategoryController(AppDbContext db) : base(db)
    {
    }

    [HttpPost("datatable")]
    [HasPermission("products.categories.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.Categories
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
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(Category.CreatedAt))
            .Skip(start)
            .Take(length)
            .Select(c => new
            {
                categoryId = c.CategoryID,
                name = c.Name,
                description = c.Description,
                parentCategoryId = c.ParentCategoryId,
                parentName = c.ParentCategory != null ? c.ParentCategory.Name : null,
                productsCount = c.ProductCategories.Count(pc => pc.IsDeleted != true),
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
                r.categoryId,
                r.name,
                r.description,
                r.parentCategoryId,
                r.parentName,
                r.productsCount,
                r.isActive,
            }),
        });
    }

    // چرا بدون HasPermission: دراپ‌داون دسته‌بندی در فرم محصول استفاده می‌شود.
    [HttpGet("list")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await Db.Categories
            .AsNoTracking()
            .Where(c => c.IsDeleted != true && c.IsActive == true)
            .OrderBy(c => c.ParentCategory != null ? c.ParentCategory.Name : c.Name)
            .ThenBy(c => c.Name)
            .Select(c => new
            {
                value = c.CategoryID,
                label = c.ParentCategory != null
                    ? $"{c.ParentCategory.Name} + {c.Name}"
                    : c.Name,
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost]
    [HasPermission("products.categories.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var name = request.Name.Trim();
        var exists = await Db.Categories
            .AnyAsync(c => c.IsDeleted != true && c.Name == name, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "دسته‌بندی با این نام قبلاً ثبت شده است." });
        }

        Db.Categories.Add(new Category
        {
            Name = name,
            Description = request.Description?.Trim(),
            ParentCategoryId = request.ParentCategoryId,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        });

        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "دسته‌بندی با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
    [HasPermission("products.categories.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = await Db.Categories
            .FirstOrDefaultAsync(c => c.CategoryID == id && c.IsDeleted != true, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "دسته‌بندی یافت نشد." });
        }

        if (request.ParentCategoryId == id)
        {
            return BadRequest(new { message = "دسته‌بندی نمی‌تواند والد خودش باشد." });
        }

        var name = request.Name.Trim();
        var exists = await Db.Categories
            .AnyAsync(c => c.IsDeleted != true && c.Name == name && c.CategoryID != id, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "دسته‌بندی با این نام قبلاً ثبت شده است." });
        }

        entity.Name = name;
        entity.Description = request.Description?.Trim();
        entity.ParentCategoryId = request.ParentCategoryId;
        entity.IsActive = request.IsActive;
        entity.IsUpdated = true;
        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "دسته‌بندی با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("products.categories.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await Db.Categories
            .FirstOrDefaultAsync(c => c.CategoryID == id && c.IsDeleted != true, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "دسته‌بندی یافت نشد." });
        }

        var inUse = await Db.ProductCategories
            .AnyAsync(pc => pc.CategoryId == id && pc.IsDeleted != true, cancellationToken);
        if (inUse)
        {
            return Conflict(new { message = "این دسته‌بندی به محصولات متصل است و قابل حذف نیست." });
        }

        var hasChildren = await Db.Categories
            .AnyAsync(c => c.ParentCategoryId == id && c.IsDeleted != true, cancellationToken);
        if (hasChildren)
        {
            return Conflict(new { message = "این دسته‌بندی دارای زیرمجموعه است و قابل حذف نیست." });
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = DateTime.Now;
        entity.DeletedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "دسته‌بندی با موفقیت حذف شد." });
    }

    public class SaveCategoryRequest
    {
        [Required(ErrorMessage = "نام الزامی است.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int? ParentCategoryId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
