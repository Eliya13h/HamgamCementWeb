using System.Security.Claims;
using HamgamTransport.Server.Data;
using Microsoft.AspNetCore.Mvc;

namespace HamgamTransport.Server.Controllers.Finance;

public abstract class FinanceControllerBase : ControllerBase
{
    protected readonly AppDbContext Db;

    protected FinanceControllerBase(AppDbContext db)
    {
        Db = db;
    }

    protected int? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
