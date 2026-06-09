using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.People;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Employees;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeeController : ControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = "FullName",
        [2] = nameof(Employee.NationalCode),
        [3] = nameof(Employee.Mobile),
        [4] = "DepartmentName",
        [5] = nameof(Employee.Sallary),
        [6] = nameof(Employee.IsActive),
    };

    private readonly AppDbContext _db;

    public EmployeeController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("datatable")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var draw = request.Draw;
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = _db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Where(e => e.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(e =>
                e.Name.Contains(searchValue) ||
                e.Family.Contains(searchValue) ||
                e.NationalCode.Contains(searchValue) ||
                e.Mobile.Contains(searchValue) ||
                (e.Department != null && e.Department.Name.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var orderedQuery = ApplyOrdering(query, request.Order);
        var rows = await orderedQuery
            .Skip(start)
            .Take(length)
            .Select(e => new EmployeeTableRow
            {
                EmployeeId = e.EmployeeID,
                Title = e.Title,
                Name = e.Name,
                FatherName = e.FatherName,
                Family = e.Family,
                NationalCode = e.NationalCode,
                Mobile = e.Mobile,
                Address = e.Address,
                Sallary = e.Sallary,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department != null ? e.Department.Name : string.Empty,
                IsActive = e.IsActive == true,
            })
            .ToListAsync(cancellationToken);

        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].RowNumber = start + i + 1;
            rows[i].FullName = $"{rows[i].Name} {rows[i].Family}".Trim();
        }

        return Ok(new
        {
            draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select(r => new
            {
                r.RowNumber,
                r.EmployeeId,
                title = (int)r.Title,
                r.Name,
                r.FatherName,
                r.Family,
                r.FullName,
                r.NationalCode,
                r.Mobile,
                r.Address,
                r.Sallary,
                r.DepartmentId,
                r.DepartmentName,
                r.IsActive,
            }),
        });
    }

    [HttpGet("departments")]
    public async Task<IActionResult> Departments(CancellationToken cancellationToken)
    {
        var departments = await _db.Departments
            .AsNoTracking()
            .Where(d => d.IsDeleted != true && d.IsActive == true)
            .OrderBy(d => d.Name)
            .Select(d => new { departmentId = d.DepartmentID, name = d.Name })
            .ToListAsync(cancellationToken);

        return Ok(departments);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var department = await _db.Departments.FirstOrDefaultAsync(
            d => d.DepartmentID == request.DepartmentId && d.IsDeleted != true && d.IsActive == true,
            cancellationToken);

        if (department is null)
        {
            return BadRequest(new { message = "بخش انتخاب‌شده معتبر نیست." });
        }

        var employee = new Employee
        {
            Title = request.Title,
            Name = request.Name.Trim(),
            FatherName = request.FatherName.Trim(),
            Family = request.Family.Trim(),
            NationalCode = request.NationalCode.Trim(),
            Mobile = request.Mobile.Trim(),
            Address = request.Address.Trim(),
            Sallary = request.Sallary,
            DepartmentId = department.DepartmentID,
            CreatedBy = ResolveCurrentUserId(),
            CreatedAt = DateTime.Now,
            IsActive = request.IsActive,
            IsDeleted = false,
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(Update),
            new { id = employee.EmployeeID },
            new { message = "کارمند با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.EmployeeID == id && e.IsDeleted != true, cancellationToken);

        if (employee is null)
        {
            return NotFound(new { message = "کارمند یافت نشد." });
        }

        var department = await _db.Departments.FirstOrDefaultAsync(
            d => d.DepartmentID == request.DepartmentId && d.IsDeleted != true && d.IsActive == true,
            cancellationToken);

        if (department is null)
        {
            return BadRequest(new { message = "بخش انتخاب‌شده معتبر نیست." });
        }

        employee.Title = request.Title;
        employee.Name = request.Name.Trim();
        employee.FatherName = request.FatherName.Trim();
        employee.Family = request.Family.Trim();
        employee.NationalCode = request.NationalCode.Trim();
        employee.Mobile = request.Mobile.Trim();
        employee.Address = request.Address.Trim();
        employee.Sallary = request.Sallary;
        employee.DepartmentId = department.DepartmentID;
        employee.IsActive = request.IsActive;
        employee.UpdatedAt = DateTime.Now;
        employee.IsUpdated = true;
        employee.UpdatedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "کارمند با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.EmployeeID == id && e.IsDeleted != true, cancellationToken);

        if (employee is null)
        {
            return NotFound(new { message = "کارمند یافت نشد." });
        }

        if (employee.User is not null && employee.User.IsDeleted != true)
        {
            return BadRequest(new { message = "این کارمند دارای حساب کاربری است و قابل حذف نیست." });
        }

        employee.IsDeleted = true;
        employee.IsActive = false;
        employee.DeletedAt = DateTime.Now;
        employee.DeletedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "کارمند با موفقیت حذف شد." });
    }

    private static IQueryable<Employee> ApplyOrdering(
        IQueryable<Employee> query,
        List<DataTableOrder>? orders)
    {
        if (orders is null || orders.Count == 0)
        {
            return query.OrderByDescending(e => e.CreatedAt);
        }

        IOrderedQueryable<Employee>? ordered = null;
        foreach (var order in orders)
        {
            if (!OrderColumns.TryGetValue(order.Column, out var column))
            {
                continue;
            }

            var descending = string.Equals(order.Dir, "desc", StringComparison.OrdinalIgnoreCase);

            ordered = column switch
            {
                "FullName" when ordered is null => descending
                    ? query.OrderByDescending(e => e.Family).ThenByDescending(e => e.Name)
                    : query.OrderBy(e => e.Family).ThenBy(e => e.Name),
                "FullName" => descending
                    ? ordered!.ThenByDescending(e => e.Family).ThenByDescending(e => e.Name)
                    : ordered!.ThenBy(e => e.Family).ThenBy(e => e.Name),
                nameof(Employee.NationalCode) when ordered is null => descending
                    ? query.OrderByDescending(e => e.NationalCode)
                    : query.OrderBy(e => e.NationalCode),
                nameof(Employee.NationalCode) => descending
                    ? ordered!.ThenByDescending(e => e.NationalCode)
                    : ordered!.ThenBy(e => e.NationalCode),
                nameof(Employee.Mobile) when ordered is null => descending
                    ? query.OrderByDescending(e => e.Mobile)
                    : query.OrderBy(e => e.Mobile),
                nameof(Employee.Mobile) => descending
                    ? ordered!.ThenByDescending(e => e.Mobile)
                    : ordered!.ThenBy(e => e.Mobile),
                "DepartmentName" when ordered is null => descending
                    ? query.OrderByDescending(e => e.Department!.Name)
                    : query.OrderBy(e => e.Department!.Name),
                "DepartmentName" => descending
                    ? ordered!.ThenByDescending(e => e.Department!.Name)
                    : ordered!.ThenBy(e => e.Department!.Name),
                nameof(Employee.Sallary) when ordered is null => descending
                    ? query.OrderByDescending(e => e.Sallary)
                    : query.OrderBy(e => e.Sallary),
                nameof(Employee.Sallary) => descending
                    ? ordered!.ThenByDescending(e => e.Sallary)
                    : ordered!.ThenBy(e => e.Sallary),
                nameof(Employee.IsActive) when ordered is null => descending
                    ? query.OrderByDescending(e => e.IsActive)
                    : query.OrderBy(e => e.IsActive),
                nameof(Employee.IsActive) => descending
                    ? ordered!.ThenByDescending(e => e.IsActive)
                    : ordered!.ThenBy(e => e.IsActive),
                _ => ordered,
            };
        }

        return ordered ?? query.OrderByDescending(e => e.CreatedAt);
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

    public class EmployeeTableRow
    {
        public int RowNumber { get; set; }
        public int EmployeeId { get; set; }
        public PersonTitle Title { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal Sallary { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class SaveEmployeeRequest
    {
        public PersonTitle Title { get; set; } = PersonTitle.Mr;

        [Required(ErrorMessage = "نام الزامی است.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FatherName { get; set; } = string.Empty;

        [Required(ErrorMessage = "نام خانوادگی الزامی است.")]
        [MaxLength(100)]
        public string Family { get; set; } = string.Empty;

        [MaxLength(20)]
        public string NationalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "موبایل الزامی است.")]
        [MaxLength(20)]
        public string Mobile { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        public decimal Sallary { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "شناسه بخش معتبر نیست.")]
        public int DepartmentId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
