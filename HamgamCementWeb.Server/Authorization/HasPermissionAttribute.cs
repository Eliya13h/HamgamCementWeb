using System.Security.Claims;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Authorization;

// چرا: تا پیش از این کنترل دسترسی فقط در فرانت انجام می‌شد و هر کاربر لاگین‌شده
// می‌توانست همه‌ی APIها را صدا بزند. این attribute یک نقطه‌ی مرکزی برای اعمال
// permission در بک‌اند فراهم می‌کند تا کلیدها دقیقاً با فرانت هماهنگ بمانند.
public sealed class HasPermissionAttribute : TypeFilterAttribute
{
    public HasPermissionAttribute(string permissionKey)
        : base(typeof(HasPermissionFilter))
    {
        Arguments = [permissionKey];
    }
}

public sealed class HasPermissionFilter : IAsyncAuthorizationFilter
{
    private readonly string _permissionKey;
    private readonly AppDbContext _db;

    public HasPermissionFilter(string permissionKey, AppDbContext db)
    {
        _permissionKey = permissionKey;
        _db = db;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var principal = context.HttpContext.User;
        if (principal?.Identity is null || !principal.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // خواندن کاربر و دسترسی‌هایش از دیتابیس؛ عمداً از DB خوانده می‌شود نه از claim
        // تا تغییر دسترسی‌ها بلافاصله اعمال شود و به توکن قدیمی وابسته نباشد.
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Permissions)
            .FirstOrDefaultAsync(
                u => u.UserID == userId && u.IsDeleted != true && u.IsActive == true,
                context.HttpContext.RequestAborted);

        if (user is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var permissionKeys = user.HasFullAccess
            ? Array.Empty<string>()
            : user.Permissions.Select(p => p.PermissionKey).ToArray();

        if (!PermissionService.HasPermission(user.HasFullAccess, permissionKeys, _permissionKey))
        {
            // نبود دسترسی → ۴۰۳ (نه ۴۰۱)؛ کاربر احراز هویت شده ولی مجاز نیست.
            context.Result = new ForbidResult();
        }
    }
}
