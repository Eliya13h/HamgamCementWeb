using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamTransport.Server.Authorization;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.People;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Controllers.People;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDepartmentReadService _reads;

    public DepartmentController(AppDbContext db, IDepartmentReadService reads)
    {
        _db = db;
        _reads = reads;
    }

    [HttpPost("datatable")]
    [HasPermission("people.departments.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reads.QueryDataTableAsync(
            new DepartmentDataTableQuery
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

    public class SaveDepartmentRequest
    {
        [Required(ErrorMessage = "نام الزامی است.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
    }
}
