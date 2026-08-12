using System.ComponentModel.DataAnnotations;
using Dapper;
using HamgamTransport.Server.Controllers.Common;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.People;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Controllers.Transport;

[ApiController]
[Route("api/transport/vehicle-owners")]
[Authorize]
public class VehicleOwnerController : TransportControllerBase
{
    private readonly ISqlConnectionFactory _sql;
    private readonly IAccountLookupService _accounts;

    public VehicleOwnerController(AppDbContext db, ISqlConnectionFactory sql, IAccountLookupService accounts) : base(db)
    {
        _sql = sql;
        _accounts = accounts;
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
            where += " AND (Name LIKE @Search OR PhoneNumber LIKE @Search)";
            p.Add("Search", $"%{search}%");
        }

        var recordsTotal = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(1) FROM VehicleOwners {baseWhere}");
        var recordsFiltered = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(1) FROM VehicleOwners {where}", p);
        p.Add("Offset", start);
        p.Add("Fetch", length);

        var rows = (await conn.QueryAsync(
            $"""
             SELECT VehicleOwnerId AS vehicleOwnerId, Name AS name, PhoneNumber AS phoneNumber,
                    City AS city, OwnerType AS ownerType, IsActive AS isActive
             FROM VehicleOwners {where}
             ORDER BY Name OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
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
                    vehicleOwnerId = d["vehicleOwnerId"],
                    name = d["name"],
                    phoneNumber = d["phoneNumber"],
                    city = d["city"],
                    ownerType = d["ownerType"],
                    isActive = d["isActive"],
                };
            }),
        });
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken ct) =>
        Ok(await Db.VehicleOwners.AsNoTracking()
            .Where(v => v.IsDeleted != true && v.IsActive == true)
            .OrderBy(v => v.Name)
            .Select(v => new { value = v.VehicleOwnerId, label = v.Name })
            .ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VehicleOwnerRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var entity = new VehicleOwner
        {
            Title = request.Title,
            Name = request.Name.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty,
            Address = request.Address?.Trim() ?? string.Empty,
            City = request.City?.Trim() ?? string.Empty,
            OwnerType = request.OwnerType,
            InitialBalance = request.InitialBalance,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };
        Db.VehicleOwners.Add(entity);
        await Db.SaveChangesAsync(ct);

        var account = await _accounts.EnsureVehicleOwnerAccountAsync(entity.VehicleOwnerId, entity.Name, ct);
        entity.AccountId = account.AccountID;
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "مالک ثبت شد.", vehicleOwnerId = entity.VehicleOwnerId });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] VehicleOwnerRequest request, CancellationToken ct)
    {
        var entity = await Db.VehicleOwners.FirstOrDefaultAsync(v => v.VehicleOwnerId == id && v.IsDeleted != true, ct);
        if (entity is null) return NotFound(new { message = "یافت نشد." });

        entity.Title = request.Title;
        entity.Name = request.Name.Trim();
        entity.PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
        entity.Address = request.Address?.Trim() ?? string.Empty;
        entity.City = request.City?.Trim() ?? string.Empty;
        entity.OwnerType = request.OwnerType;
        entity.InitialBalance = request.InitialBalance;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "به‌روزرسانی شد." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.VehicleOwners.FirstOrDefaultAsync(v => v.VehicleOwnerId == id && v.IsDeleted != true, ct);
        if (entity is null) return NotFound(new { message = "یافت نشد." });
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.DeletedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "حذف شد." });
    }
}

public class VehicleOwnerRequest
{
    public PersonTitle Title { get; set; } = PersonTitle.Mr;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string? PhoneNumber { get; set; }
    [MaxLength(500)] public string? Address { get; set; }
    [MaxLength(100)] public string? City { get; set; }
    public PersonType OwnerType { get; set; } = PersonType.NaturalPerson;
    public decimal InitialBalance { get; set; }
    public bool IsActive { get; set; } = true;
}
