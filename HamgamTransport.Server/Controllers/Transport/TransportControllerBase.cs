using System.Security.Claims;
using HamgamTransport.Server.Data;
using Microsoft.AspNetCore.Mvc;

namespace HamgamTransport.Server.Controllers.Transport;

public abstract class TransportControllerBase : ControllerBase
{
    protected readonly AppDbContext Db;

    protected TransportControllerBase(AppDbContext db)
    {
        Db = db;
    }

    protected int? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
