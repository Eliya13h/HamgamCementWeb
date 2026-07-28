using System.ComponentModel.DataAnnotations;
using Dapper;
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
[Route("api/finance/cost-centers")]
[Authorize]
public class CostCenterController : FinanceControllerBase
{
    private readonly ISqlConnectionFactory _sql;

    public CostCenterController(AppDbContext db, ISqlConnectionFactory sql) : base(db)
    {
        _sql = sql;
    }

    [HttpPost("datatable")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);
        var search = request.Search?.Value?.Trim();

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);
        const string baseWhere = "WHERE IsDeleted = 0";
        var where = baseWhere;
        var parameters = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (Code LIKE @Search OR Name LIKE @Search)";
            parameters.Add("Search", $"%{search}%");
        }

        var recordsTotal = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM CostCenters {baseWhere}");
        var recordsFiltered = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM CostCenters {where}", parameters);

        parameters.Add("Offset", start);
        parameters.Add("Fetch", length);

        var rows = (await connection.QueryAsync(
            $"""
             SELECT CostCenterID AS costCenterId, Code AS code, Name AS name,
                    Description AS description, IsActive AS isActive
             FROM CostCenters
             {where}
             ORDER BY Code
             OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
             """, parameters)).ToList();

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select((r, i) =>
            {
                var dict = (IDictionary<string, object>)r;
                return new
                {
                    rowNumber = start + i + 1,
                    costCenterId = dict["costCenterId"],
                    code = dict["code"],
                    name = dict["name"],
                    description = dict["description"],
                    isActive = dict["isActive"],
                };
            }),
        });
    }

    [HttpGet("options")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> Options(CancellationToken cancellationToken)
    {
        var rows = await Db.CostCenters.AsNoTracking()
            .Where(c => c.IsDeleted != true && c.IsActive == true)
            .OrderBy(c => c.Code)
            .Select(c => new { value = c.CostCenterID, label = c.Code + " — " + c.Name })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpPost]
    [HasPermission("accounting.expenses.create")]
    public async Task<IActionResult> Create([FromBody] CostCenterRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var code = request.Code.Trim();
        var exists = await Db.CostCenters.AnyAsync(
            c => c.IsDeleted != true && c.Code == code, cancellationToken);
        if (exists) return BadRequest(new { message = "کد مرکز هزینه تکراری است." });

        var entity = new CostCenter
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };
        Db.CostCenters.Add(entity);
        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "مرکز هزینه ثبت شد.", costCenterId = entity.CostCenterID });
    }

    [HttpPut("{id:int}")]
    [HasPermission("accounting.expenses.edit")]
    public async Task<IActionResult> Update(int id, [FromBody] CostCenterRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var entity = await Db.CostCenters.FirstOrDefaultAsync(
            c => c.CostCenterID == id && c.IsDeleted != true, cancellationToken);
        if (entity is null) return NotFound(new { message = "یافت نشد." });

        var code = request.Code.Trim();
        var dup = await Db.CostCenters.AnyAsync(
            c => c.IsDeleted != true && c.Code == code && c.CostCenterID != id, cancellationToken);
        if (dup) return BadRequest(new { message = "کد مرکز هزینه تکراری است." });

        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.IsActive = request.IsActive;
        entity.IsUpdated = true;
        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "مرکز هزینه به‌روزرسانی شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("accounting.expenses.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await Db.CostCenters.FirstOrDefaultAsync(
            c => c.CostCenterID == id && c.IsDeleted != true, cancellationToken);
        if (entity is null) return NotFound(new { message = "یافت نشد." });
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = DateTime.Now;
        entity.DeletedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "مرکز هزینه حذف شد." });
    }
}

public class CostCenterRequest
{
    [Required, MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
