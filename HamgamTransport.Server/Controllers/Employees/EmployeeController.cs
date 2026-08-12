using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.People;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Controllers.Employees;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmployeeReadService _reads;

    public EmployeeController(AppDbContext db, IEmployeeReadService reads)
    {
        _db = db;
        _reads = reads;
    }

    [HttpPost("datatable")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reads.QueryDataTableAsync(
            new EmployeeDataTableQuery
            {
                Start = request.Start,
                Length = request.Length,
                Search = request.Search?.Value,
                Order = request.Order?
                    .Select(o => new DataTableOrderItem { Column = o.Column, Dir = o.Dir })
                    .ToList(),
            },
            cancellationToken);

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Rows.Select(r => new
            {
                r.RowNumber,
                r.EmployeeId,
                title = r.Title,
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
        var departments = await _reads.ListActiveDepartmentsAsync(cancellationToken);
        return Ok(departments.Select(d => new { departmentId = d.DepartmentId, name = d.Name }));
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
