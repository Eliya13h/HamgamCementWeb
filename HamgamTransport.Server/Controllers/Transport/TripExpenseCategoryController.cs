using System.ComponentModel.DataAnnotations;
using Dapper;
using HamgamTransport.Server.Controllers.Common;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Transport;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Controllers.Transport;

[ApiController]
[Route("api/transport/trip-expense-categories")]
[Authorize]
public class TripExpenseCategoryController : TransportControllerBase
{
    private readonly ISqlConnectionFactory _sql;

    public TripExpenseCategoryController(AppDbContext db, ISqlConnectionFactory sql) : base(db)
    {
        _sql = sql;
    }

    [HttpPost("datatable")]
    public async Task<IActionResult> DataTable([FromBody] DataTableRequest request, CancellationToken ct)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);
        await using var conn = (System.Data.Common.DbConnection)await _sql.OpenAsync(ct);
        var recordsTotal = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM TripExpenseCategories WHERE IsDeleted = 0");
        var rows = (await conn.QueryAsync(
            """
            SELECT TripExpenseCategoryId AS tripExpenseCategoryId, Code AS code, Name AS name, IsActive AS isActive
            FROM TripExpenseCategories WHERE IsDeleted = 0 ORDER BY Code
            OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
            """, new { Offset = start, Fetch = length })).ToList();
        return Ok(new { request.Draw, recordsTotal, recordsFiltered = recordsTotal, data = rows.Select((r, i) => { var d = (IDictionary<string, object>)r; return new { rowNumber = start + i + 1, tripExpenseCategoryId = d["tripExpenseCategoryId"], code = d["code"], name = d["name"], isActive = d["isActive"] }; }) });
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken ct) =>
        Ok(await Db.TripExpenseCategories.AsNoTracking()
            .Where(c => c.IsDeleted != true && c.IsActive == true)
            .OrderBy(c => c.Code)
            .Select(c => new { value = c.TripExpenseCategoryId, label = c.Name })
            .ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TripExpenseCategoryRequest request, CancellationToken ct)
    {
        var entity = new TripExpenseCategory
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };
        Db.TripExpenseCategories.Add(entity);
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "ثبت شد.", tripExpenseCategoryId = entity.TripExpenseCategoryId });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TripExpenseCategoryRequest request, CancellationToken ct)
    {
        var entity = await Db.TripExpenseCategories.FirstOrDefaultAsync(c => c.TripExpenseCategoryId == id && c.IsDeleted != true, ct);
        if (entity is null) return NotFound();
        entity.Name = request.Name.Trim();
        entity.IsActive = request.IsActive;
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "به‌روزرسانی شد." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.TripExpenseCategories.FirstOrDefaultAsync(c => c.TripExpenseCategoryId == id && c.IsDeleted != true, ct);
        if (entity is null) return NotFound();
        entity.IsDeleted = true;
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "حذف شد." });
    }
}

public class TripExpenseCategoryRequest
{
    [Required] public string Code { get; set; } = string.Empty;
    [Required] public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
