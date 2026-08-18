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
[Route("api/transport/vehicle-types")]
[Authorize]
public class VehicleTypeController : TransportControllerBase
{
    private readonly ISqlConnectionFactory _sql;

    public VehicleTypeController(AppDbContext db, ISqlConnectionFactory sql) : base(db)
    {
        _sql = sql;
    }

    [HttpPost("datatable")]
    public async Task<IActionResult> DataTable([FromBody] DataTableRequest request, CancellationToken ct)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);
        var search = request.Search?.Value?.Trim();

        await using var conn = (System.Data.Common.DbConnection)await _sql.OpenAsync(ct);
        const string baseWhere = "WHERE IsDeleted = 0";
        var where = baseWhere;
        var p = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (Code LIKE @Search OR Name LIKE @Search)";
            p.Add("Search", $"%{search}%");
        }

        var recordsTotal = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(1) FROM VehicleTypes {baseWhere}");
        var recordsFiltered = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(1) FROM VehicleTypes {where}", p);
        p.Add("Offset", start);
        p.Add("Fetch", length);

        var rows = (await conn.QueryAsync(
            $"""
             SELECT VehicleTypeId AS vehicleTypeId, Code AS code, Name AS name,
                    DefaultRole AS defaultRole, IsActive AS isActive
             FROM VehicleTypes {where}
             ORDER BY Code OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
             """, p)).ToList();

        return Ok(new
        {
            request.Draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select((r, i) =>
            {
                var d = (IDictionary<string, object>)r;
                return new
                {
                    rowNumber = start + i + 1,
                    vehicleTypeId = d["vehicleTypeId"],
                    code = d["code"],
                    name = d["name"],
                    defaultRole = d["defaultRole"],
                    isActive = d["isActive"],
                };
            }),
        });
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken ct) =>
        Ok(await Db.VehicleTypes.AsNoTracking()
            .Where(v => v.IsDeleted != true && v.IsActive == true)
            .OrderBy(v => v.VehicleTypeId)
            .Select(v => new
            {
                value = v.VehicleTypeId,
                label = v.Name,
                code = v.Code,
                defaultRole = (int)v.DefaultRole,
            })
            .ToListAsync(ct));

    [HttpPost]
    public IActionResult Create() =>
        BadRequest(new { message = "انواع وسیله سیستمی هستند و از این صفحه قابل ثبت نیستند." });

    [HttpPut("{id:int}")]
    public IActionResult Update(int id) =>
        BadRequest(new { message = "انواع وسیله سیستمی هستند و قابل ویرایش نیستند." });

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id) =>
        BadRequest(new { message = "انواع وسیله سیستمی هستند و قابل حذف نیستند." });
}
