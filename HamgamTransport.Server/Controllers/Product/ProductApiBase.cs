using System.Security.Claims;
using HamgamTransport.Server.Data;
using Microsoft.AspNetCore.Mvc;

namespace HamgamTransport.Server.Controllers.Product;

public abstract class ProductControllerBase : ControllerBase
{
    protected readonly AppDbContext Db;

    protected ProductControllerBase(AppDbContext db)
    {
        Db = db;
    }

    protected int? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
