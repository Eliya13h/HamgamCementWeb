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
            SELECT c.TripExpenseCategoryId AS tripExpenseCategoryId, c.Code AS code, c.Name AS name,
                   c.ParentCategoryId AS parentCategoryId, p.Name AS parentName, c.IsActive AS isActive
            FROM TripExpenseCategories c
            LEFT JOIN TripExpenseCategories p ON p.TripExpenseCategoryId = c.ParentCategoryId AND p.IsDeleted = 0
            WHERE c.IsDeleted = 0
            ORDER BY ISNULL(p.Code, c.Code), c.Code
            OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
            """, new { Offset = start, Fetch = length })).ToList();
        return Ok(new
        {
            request.Draw,
            recordsTotal,
            recordsFiltered = recordsTotal,
            data = rows.Select((r, i) =>
            {
                var d = (IDictionary<string, object>)r;
                return new
                {
                    rowNumber = start + i + 1,
                    tripExpenseCategoryId = d["tripExpenseCategoryId"],
                    code = d["code"],
                    name = d["name"],
                    parentCategoryId = d["parentCategoryId"],
                    parentName = d["parentName"],
                    isActive = d["isActive"],
                };
            }),
        });
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken ct)
    {
        var cats = await Db.TripExpenseCategories.AsNoTracking()
            .Where(c => c.IsDeleted != true && c.IsActive == true)
            .Select(c => new { c.TripExpenseCategoryId, c.Code, c.Name, c.ParentCategoryId })
            .ToListAsync(ct);

        var byId = cats.ToDictionary(c => c.TripExpenseCategoryId);
        string Label(int id, int guard = 0)
        {
            if (!byId.TryGetValue(id, out var cat) || guard > 8) return "";
            if (cat.ParentCategoryId is int parentId && byId.ContainsKey(parentId))
            {
                return $"{Label(parentId, guard + 1)} / {cat.Name}";
            }
            return cat.Name;
        }

        return Ok(cats
            .OrderBy(c => Label(c.TripExpenseCategoryId))
            .Select(c => new { value = c.TripExpenseCategoryId, label = Label(c.TripExpenseCategoryId) }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TripExpenseCategoryRequest request, CancellationToken ct)
    {
        if (request.ParentCategoryId is int parentId)
        {
            if (!await Db.TripExpenseCategories.AnyAsync(c => c.TripExpenseCategoryId == parentId && c.IsDeleted != true, ct))
                return BadRequest(new { message = "دسته والد یافت نشد." });
        }

        var entity = new TripExpenseCategory
        {
            Code = string.IsNullOrWhiteSpace(request.Code) ? "TMP" : request.Code.Trim(),
            Name = request.Name.Trim(),
            ParentCategoryId = request.ParentCategoryId,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };
        Db.TripExpenseCategories.Add(entity);
        await Db.SaveChangesAsync(ct);
        if (entity.Code == "TMP")
        {
            entity.Code = $"EX-{entity.TripExpenseCategoryId:D4}";
            await Db.SaveChangesAsync(ct);
        }
        return Ok(new { message = "ثبت شد.", tripExpenseCategoryId = entity.TripExpenseCategoryId });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TripExpenseCategoryRequest request, CancellationToken ct)
    {
        var entity = await Db.TripExpenseCategories.FirstOrDefaultAsync(c => c.TripExpenseCategoryId == id && c.IsDeleted != true, ct);
        if (entity is null) return NotFound();
        if (request.ParentCategoryId == id)
            return BadRequest(new { message = "دسته نمی‌تواند والد خودش باشد." });
        if (request.ParentCategoryId is int parentId)
        {
            var walk = parentId;
            var guard = 0;
            while (walk > 0 && guard++ < 16)
            {
                if (walk == id) return BadRequest(new { message = "حلقه در سلسله‌مراتب دسته مجاز نیست." });
                walk = await Db.TripExpenseCategories
                    .Where(c => c.TripExpenseCategoryId == walk)
                    .Select(c => c.ParentCategoryId ?? 0)
                    .FirstOrDefaultAsync(ct);
            }
        }

        entity.Name = request.Name.Trim();
        entity.ParentCategoryId = request.ParentCategoryId;
        entity.IsActive = request.IsActive;
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "به‌روزرسانی شد." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.TripExpenseCategories.FirstOrDefaultAsync(c => c.TripExpenseCategoryId == id && c.IsDeleted != true, ct);
        if (entity is null) return NotFound();
        var hasChildren = await Db.TripExpenseCategories.AnyAsync(
            c => c.ParentCategoryId == id && c.IsDeleted != true, ct);
        if (hasChildren) return BadRequest(new { message = "ابتدا زیردسته‌ها را حذف کنید." });
        entity.IsDeleted = true;
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "حذف شد." });
    }
}

public class TripExpenseCategoryRequest
{
    public string? Code { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}
