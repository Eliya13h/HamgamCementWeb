using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.People;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.People;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentController : ControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(Department.Name),
        [2] = nameof(Department.Description),
        [3] = "EmployeeCount",
    };

    private readonly AppDbContext _db;

    public DepartmentController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("datatable")]
    [HasPermission("people.departments.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var draw = request.Draw;
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = _db.Departments
            .AsNoTracking()
            .Where(d => d.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(d =>
                d.Name.Contains(searchValue) ||
                d.Description.Contains(searchValue));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var projected = query.Select(d => new DepartmentTableRow
        {
            DepartmentId = d.DepartmentID,
            Name = d.Name,
            Description = d.Description,
            EmployeeCount = d.Employees.Count(e => e.IsDeleted != true),
        });

        projected = ApplyOrdering(projected, request.Order);

        var rows = await projected
            .Skip(start)
            .Take(length)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].RowNumber = start + i + 1;
        }

        return Ok(new
        {
            draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select(r => new
            {
                r.RowNumber,
                r.DepartmentId,
                r.Name,
                r.Description,
                r.EmployeeCount,
            }),
        });
    }

    [HttpPost]
    [HasPermission("people.departments.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var department = new Department
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            CreatedBy = ResolveCurrentUserId(),
            CreatedAt = DateTime.Now,
            IsDeleted = false,
        };

        _db.Departments.Add(department);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(Update),
            new { id = department.DepartmentID },
            new { message = "بخش با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
    [HasPermission("people.departments.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var department = await _db.Departments
            .FirstOrDefaultAsync(d => d.DepartmentID == id && d.IsDeleted != true, cancellationToken);

        if (department is null)
        {
            return NotFound(new { message = "بخش یافت نشد." });
        }

        department.Name = request.Name.Trim();
        department.Description = request.Description.Trim();
        department.UpdatedAt = DateTime.Now;
        department.IsUpdated = true;
        department.UpdatedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "بخش با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("people.departments.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var department = await _db.Departments
            .FirstOrDefaultAsync(d => d.DepartmentID == id && d.IsDeleted != true, cancellationToken);

        if (department is null)
        {
            return NotFound(new { message = "بخش یافت نشد." });
        }

        department.IsDeleted = true;
        department.DeletedAt = DateTime.Now;
        department.DeletedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "بخش با موفقیت حذف شد." });
    }

    private static IQueryable<DepartmentTableRow> ApplyOrdering(
        IQueryable<DepartmentTableRow> query,
        List<DataTableOrder>? orders)
    {
        if (orders is null || orders.Count == 0)
        {
            return query.OrderBy(d => d.Name);
        }

        IOrderedQueryable<DepartmentTableRow>? ordered = null;
        foreach (var order in orders)
        {
            if (!OrderColumns.TryGetValue(order.Column, out var column))
            {
                continue;
            }

            var descending = string.Equals(order.Dir, "desc", StringComparison.OrdinalIgnoreCase);

            ordered = column switch
            {
                nameof(Department.Name) when ordered is null => descending
                    ? query.OrderByDescending(d => d.Name)
                    : query.OrderBy(d => d.Name),
                nameof(Department.Name) => descending
                    ? ordered!.ThenByDescending(d => d.Name)
                    : ordered!.ThenBy(d => d.Name),
                nameof(Department.Description) when ordered is null => descending
                    ? query.OrderByDescending(d => d.Description)
                    : query.OrderBy(d => d.Description),
                nameof(Department.Description) => descending
                    ? ordered!.ThenByDescending(d => d.Description)
                    : ordered!.ThenBy(d => d.Description),
                "EmployeeCount" when ordered is null => descending
                    ? query.OrderByDescending(d => d.EmployeeCount)
                    : query.OrderBy(d => d.EmployeeCount),
                "EmployeeCount" => descending
                    ? ordered!.ThenByDescending(d => d.EmployeeCount)
                    : ordered!.ThenBy(d => d.EmployeeCount),
                _ => ordered,
            };
        }

        return ordered ?? query.OrderBy(d => d.Name);
    }

    private int? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public DataTableSearch? Search { get; set; }
        public List<DataTableOrder>? Order { get; set; }
    }

    public class DataTableSearch
    {
        public string? Value { get; set; }
        public bool Regex { get; set; }
    }

    public class DataTableOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; } = "asc";
    }

    public class DepartmentTableRow
    {
        public int RowNumber { get; set; }
        public int DepartmentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
    }

    public class SaveDepartmentRequest
    {
        [Required(ErrorMessage = "نام الزامی است.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
    }
}
