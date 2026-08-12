using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamTransport.Server.Authorization;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.People;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppUser = HamgamTransport.Server.Data.Models.People.User;

namespace HamgamTransport.Server.Controllers.User;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public AuthController(AppDbContext db, IPasswordHasher<AppUser> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var normalizedUserName = request.UserName.Trim();

        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Permissions)
            .FirstOrDefaultAsync(
                u => u.UserName == normalizedUserName && u.IsDeleted != true && u.IsActive == true,
                cancellationToken);

        if (user is null)
        {
            return Unauthorized(new { message = "نام کاربری یا رمز عبور اشتباه است." });
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "نام کاربری یا رمز عبور اشتباه است." });
        }

        await SignInUserAsync(user);
        return Ok(MapToResponse(user));
    }

    // چرا: پیش‌تر این endpoint با [AllowAnonymous] بود و هر فرد ناشناسی می‌توانست
    // کاربر بسازد. حالا فقط کاربرِ لاگین‌شده‌ای که دسترسی ساخت کاربر (users.list.create)
    // دارد مجاز است؛ کلید دقیقاً با صفحه‌ی کاربران در فرانت هماهنگ است.
    [HttpPost("register")]
    [Authorize]
    [HasPermission("users.list.create")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var normalizedUserName = request.UserName.Trim();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedFullName = request.FullName.Trim();

        var userNameExists = await _db.Users.AnyAsync(
            u => u.UserName == normalizedUserName && u.IsDeleted != true,
            cancellationToken);

        if (userNameExists)
        {
            return Conflict(new { message = "این نام کاربری قبلاً ثبت شده است." });
        }

        var emailExists = await _db.Users.AnyAsync(
            u => u.Email.ToLower() == normalizedEmail && u.IsDeleted != true,
            cancellationToken);

        if (emailExists)
        {
            return Conflict(new { message = "این ایمیل قبلاً ثبت شده است." });
        }

        var employee = await _db.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(
                e => e.EmployeeID == request.EmployeeId && e.IsDeleted != true && e.IsActive == true,
                cancellationToken);

        if (employee is null)
        {
            return BadRequest(new { message = "کارمند مورد نظر یافت نشد یا غیرفعال است." });
        }

        if (employee.User is not null && employee.User.IsDeleted != true)
        {
            return Conflict(new { message = "برای این کارمند قبلاً حساب کاربری ایجاد شده است." });
        }

        var role = await _db.Roles.FirstOrDefaultAsync(
            r => r.RoleID == request.RoleId && r.IsDeleted != true && r.IsActive == true,
            cancellationToken);

        if (role is null)
        {
            return BadRequest(new { message = "نقش انتخاب‌شده معتبر نیست." });
        }

        // ثبت‌نام فقط توسط کاربر احراز هویت‌شده انجام می‌شود؛ سازنده از شناسه کاربر جاری گرفته می‌شود
        var createdBy = ResolveCreatedBy(employee);

        var user = new AppUser
        {
            UserName = normalizedUserName,
            FullName = normalizedFullName,
            Email = normalizedEmail,
            Title = request.Title,
            RoleId = role.RoleID,
            EmployeeId = employee.EmployeeID,
            AvatarUrl = employee.AvatarUrl,
            PasswordHash = _passwordHasher.HashPassword(null!, request.Password),
            CreatedBy = createdBy,
            CreatedAt = DateTime.Now,
            IsActive = true,
            IsDeleted = false,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(user).Reference(u => u.Role).LoadAsync(cancellationToken);
        await _db.Entry(user).Collection(u => u.Permissions).LoadAsync(cancellationToken);

        return CreatedAtAction(nameof(Me), MapToResponse(user));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "خروج با موفقیت انجام شد." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Permissions)
            .FirstOrDefaultAsync(
                u => u.UserID == userId && u.IsDeleted != true && u.IsActive == true,
                cancellationToken);

        if (user is null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Unauthorized();
        }

        return Ok(MapToResponse(user));
    }

    private async Task SignInUserAsync(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.Name),
            new("user_name", user.UserName),
            new("role_id", user.RoleId.ToString()),
        };

        if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            claims.Add(new Claim("avatar_url", user.AvatarUrl));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
            });
    }

    private int? ResolveCreatedBy(Employee employee)
    {
        var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(currentUserIdClaim, out var currentUserId))
        {
            return currentUserId;
        }

        return employee.CreatedBy;
    }

    private static object MapToResponse(AppUser user) => new
    {
        userId = user.UserID,
        userName = user.UserName,
        fullName = user.FullName,
        email = user.Email,
        avatarUrl = user.AvatarUrl,
        roleId = user.RoleId,
        roleName = user.Role.Name,
        hasFullAccess = user.HasFullAccess,
        permissions = user.HasFullAccess
            ? Array.Empty<string>()
            : user.Permissions.Select(p => p.PermissionKey).ToArray(),
    };

    public class LoginRequest
    {
        [Required(ErrorMessage = "نام کاربری الزامی است.")]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور الزامی است.")]
        [MaxLength(200)]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest : IValidatableObject
    {
        [Required(ErrorMessage = "نام کاربری الزامی است.")]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور الزامی است.")]
        [MinLength(6, ErrorMessage = "رمز عبور باید حداقل ۶ کاراکتر باشد.")]
        [MaxLength(200)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تکرار رمز عبور الزامی است.")]
        [MaxLength(200)]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "نام کامل الزامی است.")]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "ایمیل الزامی است.")]
        [EmailAddress(ErrorMessage = "فرمت ایمیل معتبر نیست.")]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "شناسه کارمند معتبر نیست.")]
        public int EmployeeId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "شناسه نقش معتبر نیست.")]
        public int RoleId { get; set; }

        public PersonTitle Title { get; set; } = PersonTitle.Mr;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
            {
                yield return new ValidationResult(
                    "رمز عبور و تکرار آن یکسان نیستند.",
                    [nameof(Password), nameof(ConfirmPassword)]);
            }
        }
    }
}
