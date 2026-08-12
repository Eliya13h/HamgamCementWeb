using System.ComponentModel.DataAnnotations;
using Dapper;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Controllers.Common;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/bank-accounts")]
[Authorize]
public class BankAccountController : FinanceControllerBase
{
    private readonly IBankAccountService _banks;
    private readonly ISqlConnectionFactory _sql;

    public BankAccountController(
        AppDbContext db,
        IBankAccountService banks,
        ISqlConnectionFactory sql) : base(db)
    {
        _banks = banks;
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
        const string baseWhere = "WHERE b.IsDeleted = 0";
        var where = baseWhere;
        var parameters = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (b.Code LIKE @Search OR b.Name LIKE @Search OR ISNULL(b.AccountNumber, '') LIKE @Search)";
            parameters.Add("Search", $"%{search}%");
        }

        var recordsTotal = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM BankAccounts b {baseWhere}");
        var recordsFiltered = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM BankAccounts b {where}", parameters);

        parameters.Add("Offset", start);
        parameters.Add("Fetch", length);

        var rows = (await connection.QueryAsync(
            $"""
             SELECT b.BankAccountID AS bankAccountId,
                    b.Code AS code,
                    b.Name AS name,
                    b.AccountNumber AS accountNumber,
                    b.CurrencyId AS currencyId,
                    cur.CurrencyCode AS currencyCode,
                    b.Description AS description,
                    b.IsActive AS isActive
             FROM BankAccounts b
             LEFT JOIN Currencies cur ON cur.CurrencyID = b.CurrencyId AND cur.IsDeleted = 0
             {where}
             ORDER BY b.Code
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
                    bankAccountId = dict["bankAccountId"],
                    code = dict["code"],
                    name = dict["name"],
                    accountNumber = dict["accountNumber"],
                    currencyId = dict["currencyId"],
                    currencyCode = dict["currencyCode"],
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
        var items = await Db.BankAccounts
            .AsNoTracking()
            .Where(b => b.IsDeleted != true && b.IsActive == true)
            .OrderBy(b => b.Code)
            .Select(b => new { value = b.BankAccountID, label = b.Code + " — " + b.Name })
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var bank = await Db.BankAccounts
            .AsNoTracking()
            .Where(b => b.BankAccountID == id && b.IsDeleted != true)
            .Select(b => new
            {
                bankAccountId = b.BankAccountID,
                code = b.Code,
                name = b.Name,
                accountNumber = b.AccountNumber,
                currencyId = b.CurrencyId,
                description = b.Description,
                isActive = b.IsActive == true,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return bank is null ? NotFound(new { message = "حساب بانکی یافت نشد." }) : Ok(bank);
    }

    [HttpPost]
    [HasPermission("accounting.expenses.create")]
    public async Task<IActionResult> Create([FromBody] SaveBankAccountRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var bank = await _banks.CreateAsync(
                request.Code,
                request.Name,
                request.AccountNumber,
                request.CurrencyId,
                request.Description,
                ResolveCurrentUserId(),
                cancellationToken);
            return Ok(new { message = "حساب بانکی ثبت شد.", bankAccountId = bank.BankAccountID, code = bank.Code });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [HasPermission("accounting.expenses.edit")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveBankAccountRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            await _banks.UpdateAsync(
                id,
                request.Name,
                request.AccountNumber,
                request.CurrencyId,
                request.Description,
                request.IsActive ?? true,
                ResolveCurrentUserId(),
                cancellationToken);
            return Ok(new { message = "حساب بانکی به‌روزرسانی شد." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class SaveBankAccountRequest
{
    [MaxLength(30)]
    public string? Code { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? AccountNumber { get; set; }

    public int? CurrencyId { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool? IsActive { get; set; } = true;
}
