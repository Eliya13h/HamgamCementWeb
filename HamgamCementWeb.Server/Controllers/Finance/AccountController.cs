using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/accounts")]
[Authorize]
public class AccountController : FinanceControllerBase
{
    private readonly IFinanceReadService _reads;

    public AccountController(AppDbContext db, IFinanceReadService reads) : base(db)
    {
        _reads = reads;
    }

    [HttpGet("tree")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> Tree(CancellationToken cancellationToken)
    {
        var accounts = await _reads.GetAccountTreeAsync(cancellationToken);
        return Ok(accounts.Select(a => new
        {
            accountId = a.AccountId,
            code = a.Code,
            name = a.Name,
            level = a.Level,
            parentAccountId = a.ParentAccountId,
            accountType = a.AccountType,
            nature = a.Nature,
            isPostable = a.IsPostable,
            isSystem = a.IsSystem,
            systemCode = a.SystemCode,
        }));
    }

    [HttpGet("{id:int}")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var account = await Db.Accounts
            .AsNoTracking()
            .Where(a => a.AccountID == id && a.IsDeleted != true)
            .Select(a => new
            {
                accountId = a.AccountID,
                code = a.Code,
                name = a.Name,
                level = (int)a.Level,
                parentAccountId = a.ParentAccountId,
                accountType = a.AccountType,
                nature = a.Nature,
                isPostable = a.IsPostable,
                isSystem = a.IsSystem,
                description = a.Description,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return account is null ? NotFound(new { message = "حساب یافت نشد." }) : Ok(account);
    }
}
