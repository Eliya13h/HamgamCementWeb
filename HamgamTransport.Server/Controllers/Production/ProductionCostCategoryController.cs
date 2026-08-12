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
[Route("api/production/cost-categories")]
[Authorize]
public class ProductionCostCategoryController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IProductionCostCategoryReadService _read;

    public ProductionCostCategoryController(AppDbContext db, IProductionCostCategoryReadService read)
    {
        _db = db;
        _read = read;
    }

    private int? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    [HttpPost("datatable")]
    [HasPermission("accounting.production-cost-categories.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var (recordsTotal, recordsFiltered, rows) = await _read.GetDataTableAsync(
            start,
            length,
            request.Search?.Value,
            cancellationToken);

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select((r, i) => new
            {
                rowNumber = start + i + 1,
                productionCostCategoryId = r.ProductionCostCategoryId,
                name = r.Name,
                code = r.Code,
                description = r.Description,
                isSystem = r.IsSystem,
                costType = r.CostType,
                costTypeLabel = CostTypeLabel(r.CostType),
                sortOrder = r.SortOrder,
                isActive = r.IsActive,
                departmentsCount = r.DepartmentsCount,
                departmentNamesText = r.DepartmentNamesText,
                departmentIdsText = r.DepartmentIdsText,
                // برای multiselect فرم ویرایش
                departmentIds = string.IsNullOrWhiteSpace(r.DepartmentIdsText)
                    ? Array.Empty<int>()
                    : r.DepartmentIdsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(int.Parse)
                        .ToArray(),
            }),
        });
    }

    // لیست برای فرمول تولید و فرم‌ها — فقط احراز هویت
    [HttpGet("list")]
    [Authorize]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await _read.GetListAsync(activeOnly: true, cancellationToken);
        return Ok(items.Select(c => new
        {
            value = c.Value,
            label = c.Label,
            isSystem = c.IsSystem,
            costType = c.CostType,
            code = c.Code,
        }));
    }

    [HttpGet("{id:int}")]
    [HasPermission("accounting.production-cost-categories.view")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var row = await _read.GetByIdAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound(new { message = "دسته‌بندی هزینه تولید یافت نشد." });
        }

        return Ok(new
        {
            productionCostCategoryId = row.ProductionCostCategoryId,
            name = row.Name,
            code = row.Code,
            description = row.Description,
            isSystem = row.IsSystem,
            costType = row.CostType,
            sortOrder = row.SortOrder,
            isActive = row.IsActive,
            departmentIds = row.DepartmentIds,
        });
    }

    [HttpPost]
    [HasPermission("accounting.production-cost-categories.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveProductionCostCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var name = request.Name.Trim();
        var exists = await _db.ProductionCostCategories
            .AnyAsync(c => c.IsDeleted != true && c.Name == name, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "دسته‌بندی با این نام قبلاً ثبت شده است." });
        }

        if (request.CostType is ProductionCostType.DirectWage or ProductionCostType.Overhead)
        {
            return BadRequest(new { message = "نوع هزینه سیستمی فقط برای دسته‌های سیستمی مجاز است." });
        }

        var entity = new ProductionCostCategory
        {
            Name = name,
            Description = request.Description?.Trim(),
            IsSystem = false,
            CostType = request.CostType,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };

        _db.ProductionCostCategories.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "دسته‌بندی هزینه تولید ایجاد شد.",
            productionCostCategoryId = entity.ProductionCostCategoryID,
        });
    }

    [HttpPut("{id:int}")]
    [HasPermission("accounting.production-cost-categories.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveProductionCostCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var category = await _db.ProductionCostCategories
            .Include(c => c.Departments)
            .FirstOrDefaultAsync(c => c.ProductionCostCategoryID == id && c.IsDeleted != true, cancellationToken);

        if (category is null)
        {
            return NotFound(new { message = "دسته‌بندی یافت نشد." });
        }

        var name = request.Name.Trim();
        var exists = await _db.ProductionCostCategories.AnyAsync(
            c => c.IsDeleted != true && c.Name == name && c.ProductionCostCategoryID != id,
            cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "دسته‌بندی با این نام قبلاً ثبت شده است." });
        }

        // برای سیستمی فقط نام/توضیح/فعال و بخش‌ها قابل ویرایش است
        category.Name = name;
        category.Description = request.Description?.Trim();
        category.IsActive = request.IsActive;
        category.IsUpdated = true;
        category.UpdatedAt = DateTime.Now;
        category.UpdatedBy = ResolveCurrentUserId();

        if (!category.IsSystem)
        {
            if (request.CostType is ProductionCostType.DirectWage or ProductionCostType.Overhead)
            {
                return BadRequest(new { message = "نوع هزینه سیستمی فقط برای دسته‌های سیستمی مجاز است." });
            }

            category.CostType = request.CostType;
            category.SortOrder = request.SortOrder;
        }
        else
        {
            // فقط برای مستقیم/غیرمستقیم بخش‌ها را همگام کن
            var deptIds = (request.DepartmentIds ?? [])
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (deptIds.Count > 0)
            {
                var validCount = await _db.Departments.CountAsync(
                    d => deptIds.Contains(d.DepartmentID) && d.IsDeleted != true,
                    cancellationToken);
                if (validCount != deptIds.Count)
                {
                    return BadRequest(new { message = "یکی از بخش‌های انتخاب‌شده معتبر نیست." });
                }
            }

            _db.ProductionCostCategoryDepartments.RemoveRange(category.Departments);
            foreach (var deptId in deptIds)
            {
                category.Departments.Add(new ProductionCostCategoryDepartment
                {
                    ProductionCostCategoryId = category.ProductionCostCategoryID,
                    DepartmentId = deptId,
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "دسته‌بندی هزینه تولید ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("accounting.production-cost-categories.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var category = await _db.ProductionCostCategories
            .FirstOrDefaultAsync(c => c.ProductionCostCategoryID == id && c.IsDeleted != true, cancellationToken);

        if (category is null)
        {
            return NotFound(new { message = "دسته‌بندی یافت نشد." });
        }

        if (category.IsSystem)
        {
            return Conflict(new { message = "دسته‌بندی‌های سیستمی قابل حذف نیستند." });
        }

        var inUse = await _db.ProductionFormulaCostLines.AnyAsync(
            l => l.ProductionCostCategoryId == id && l.IsDeleted != true,
            cancellationToken);
        if (inUse)
        {
            return Conflict(new { message = "این دسته‌بندی در فرمول تولید استفاده شده و قابل حذف نیست." });
        }

        category.IsDeleted = true;
        category.IsActive = false;
        category.DeletedAt = DateTime.Now;
        category.DeletedBy = ResolveCurrentUserId();
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "دسته‌بندی هزینه تولید حذف شد." });
    }

    private static string CostTypeLabel(int costType) => ((ProductionCostType)costType) switch
    {
        ProductionCostType.DirectWage => "هزینه تولید مستقیم",
        ProductionCostType.Overhead => "هزینه تولید غیر مستقیم",
        ProductionCostType.Ancillary => "هزینه جانبی",
        ProductionCostType.Fixed => "هزینه ثابت",
        ProductionCostType.ProductionBurden => "سربار تولید",
        _ => costType.ToString(),
    };

    public class SaveProductionCostCategoryRequest
    {
        [Required(ErrorMessage = "نام دسته‌بندی الزامی است.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public ProductionCostType CostType { get; set; } = ProductionCostType.Ancillary;

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public List<int>? DepartmentIds { get; set; }
    }
}
