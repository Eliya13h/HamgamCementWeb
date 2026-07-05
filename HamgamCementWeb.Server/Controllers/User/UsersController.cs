using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.People;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppUser = HamgamCementWeb.Server.Data.Models.People.User;

namespace HamgamCementWeb.Server.Controllers.User;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(AppUser.FullName),
        [2] = nameof(AppUser.UserName),
        [3] = nameof(AppUser.Email),
        [4] = "RoleName",
        [5] = nameof(AppUser.IsActive),
    };

    private readonly AppDbContext _db;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public UsersController(AppDbContext db, IPasswordHasher<AppUser> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("datatable")]
    [HasPermission("users.list.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var draw = request.Draw;
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.IsDeleted != true && u.Role != null);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(u =>
                u.UserName.Contains(searchValue) ||
                u.FullName.Contains(searchValue) ||
                u.Email.Contains(searchValue) ||
                u.Role.Name.Contains(searchValue));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var orderedQuery = ApplyOrdering(query, request.Order);
        var rows = await orderedQuery
            .Skip(start)
            .Take(length)
            .Select(u => new UserTableRow
            {
                UserId = u.UserID,
                UserName = u.UserName,
                FullName = u.FullName,
                Email = u.Email,
                RoleId = u.RoleId,
                RoleName = u.Role.Name,
                HasFullAccess = u.HasFullAccess,
                IsActive = u.IsActive == true,
                CreatedAt = u.CreatedAt,
                Title = u.Title,
            })
            .ToListAsync(cancellationToken);

        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].RowNumber = start + i + 1;
        }

        return Ok(new
        {
            draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select(r => new
            {
                r.RowNumber,
                r.UserId,
                r.UserName,
                r.FullName,
                r.Email,
                r.RoleId,
                r.RoleName,
                r.HasFullAccess,
                r.IsActive,
                r.Title,
            }),
        });
    }

    [HttpGet("available-employees")]
    [HasPermission("users.list.view")]
    public async Task<IActionResult> AvailableEmployees(CancellationToken cancellationToken)
    {
        var employees = await _db.Employees
            .AsNoTracking()
            .Where(e => e.IsDeleted != true && e.IsActive == true)
            .Where(e => e.User == null || e.User.IsDeleted == true)
            .OrderBy(e => e.Name)
            .ThenBy(e => e.Family)
            .Select(e => new
            {
                employeeId = e.EmployeeID,
                fullName = e.Name + " " + e.Family,
            })
            .ToListAsync(cancellationToken);

        return Ok(employees);
    }

    [HttpPost]
    [HasPermission("users.list.create")]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var normalizedUserName = request.UserName.Trim();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

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

        var user = new AppUser
        {
            UserName = normalizedUserName,
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            Title = request.Title,
            RoleId = role.RoleID,
            EmployeeId = employee.EmployeeID,
            AvatarUrl = employee.AvatarUrl,
            PasswordHash = _passwordHasher.HashPassword(null!, request.Password),
            CreatedBy = ResolveCurrentUserId(),
            CreatedAt = DateTime.Now,
            IsActive = request.IsActive,
            IsDeleted = false,
            HasFullAccess = true,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Update), new { id = user.UserID }, new { message = "کاربر با موفقیت ایجاد شد." });
    }

    [HttpGet("roles")]
    [HasPermission("users.list.view")]
    public async Task<IActionResult> Roles(CancellationToken cancellationToken)
    {
        var roles = await _db.Roles
            .AsNoTracking()
            .Where(r => r.IsDeleted != true && r.IsActive == true)
            .OrderBy(r => r.Name)
            .Select(r => new { roleId = r.RoleID, name = r.Name })
            .ToListAsync(cancellationToken);

        return Ok(roles);
    }

    [HttpGet("{id:int}/permissions")]
    [HasPermission("users.roles.view")]
    public async Task<IActionResult> GetPermissions(int id, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Permissions)
            .FirstOrDefaultAsync(u => u.UserID == id && u.IsDeleted != true, cancellationToken);

        if (user is null)
        {
            return NotFound(new { message = "کاربر یافت نشد." });
        }

        return Ok(new
        {
            userId = user.UserID,
            fullName = user.FullName,
            userName = user.UserName,
            roleName = user.Role.Name,
            hasFullAccess = user.HasFullAccess,
            permissions = user.HasFullAccess
                ? Array.Empty<string>()
                : user.Permissions.Select(p => p.PermissionKey).ToArray(),
        });
    }

    // چرا: مدیریت سطح دسترسی از صفحه‌ی «سطح دسترسی» (users.roles) انجام می‌شود
    // پس عملیات ذخیره به extra action مربوطه یعنی users.roles.manage نگاشت می‌شود.
    [HttpPut("{id:int}/permissions")]
    [HasPermission("users.roles.manage")]
    public async Task<IActionResult> UpdatePermissions(
        int id,
        [FromBody] UpdateUserPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = await _db.Users
            .Include(u => u.Permissions)
            .FirstOrDefaultAsync(u => u.UserID == id && u.IsDeleted != true, cancellationToken);

        if (user is null)
        {
            return NotFound(new { message = "کاربر یافت نشد." });
        }

        var permissionKeys = NormalizePermissionKeys(request);
        if (permissionKeys is null)
        {
            return BadRequest(new { message = "یک یا چند کلید دسترسی نامعتبر است." });
        }

        user.HasFullAccess = request.HasFullAccess;
        user.UpdatedAt = DateTime.Now;
        user.IsUpdated = true;
        user.UpdatedBy = ResolveCurrentUserId();

        _db.UserPermissions.RemoveRange(user.Permissions);

        if (!user.HasFullAccess && permissionKeys.Count > 0)
        {
            _db.UserPermissions.AddRange(permissionKeys.Select(key => new UserPermission
            {
                UserId = user.UserID,
                PermissionKey = key,
            }));
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "سطح دسترسی کاربر با موفقیت ذخیره شد." });
    }

    [HttpPut("{id:int}")]
    [HasPermission("users.list.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserID == id && u.IsDeleted != true, cancellationToken);

        if (user is null)
        {
            return NotFound(new { message = "کاربر یافت نشد." });
        }

        var normalizedUserName = request.UserName.Trim();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var userNameExists = await _db.Users.AnyAsync(
            u => u.UserID != id && u.UserName == normalizedUserName && u.IsDeleted != true,
            cancellationToken);

        if (userNameExists)
        {
            return Conflict(new { message = "این نام کاربری قبلاً ثبت شده است." });
        }

        var emailExists = await _db.Users.AnyAsync(
            u => u.UserID != id && u.Email.ToLower() == normalizedEmail && u.IsDeleted != true,
            cancellationToken);

        if (emailExists)
        {
            return Conflict(new { message = "این ایمیل قبلاً ثبت شده است." });
        }

        var role = await _db.Roles.FirstOrDefaultAsync(
            r => r.RoleID == request.RoleId && r.IsDeleted != true && r.IsActive == true,
            cancellationToken);

        if (role is null)
        {
            return BadRequest(new { message = "نقش انتخاب‌شده معتبر نیست." });
        }

        user.UserName = normalizedUserName;
        user.FullName = request.FullName.Trim();
        user.Email = normalizedEmail;
        user.Title = request.Title;
        user.RoleId = role.RoleID;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.Now;
        user.IsUpdated = true;
        user.UpdatedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "کاربر با موفقیت ویرایش شد." });
    }

    [HttpPut("{id:int}/password")]
    [HasPermission("users.list.changePassword")]
    public async Task<IActionResult> ChangePassword(
        int id,
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserID == id && u.IsDeleted != true, cancellationToken);

        if (user is null)
        {
            return NotFound(new { message = "کاربر یافت نشد." });
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        user.UpdatedAt = DateTime.Now;
        user.IsUpdated = true;
        user.UpdatedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "رمز عبور با موفقیت تغییر کرد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("users.list.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserID == id && u.IsDeleted != true, cancellationToken);

        if (user is null)
        {
            return NotFound(new { message = "کاربر یافت نشد." });
        }

        var currentUserId = ResolveCurrentUserId();
        if (currentUserId == id)
        {
            return BadRequest(new { message = "امکان حذف حساب کاربری فعلی وجود ندارد." });
        }

        user.IsDeleted = true;
        user.IsActive = false;
        user.DeletedAt = DateTime.Now;
        user.DeletedBy = currentUserId;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "کاربر با موفقیت حذف شد." });
    }

    private static IQueryable<AppUser> ApplyOrdering(
        IQueryable<AppUser> query,
        List<DataTableOrder>? orders)
    {
        if (orders is null || orders.Count == 0)
        {
            return query.OrderByDescending(u => u.CreatedAt);
        }

        IOrderedQueryable<AppUser>? ordered = null;
        foreach (var order in orders)
        {
            if (!OrderColumns.TryGetValue(order.Column, out var column))
            {
                continue;
            }

            var descending = string.Equals(order.Dir, "desc", StringComparison.OrdinalIgnoreCase);

            ordered = column switch
            {
                "RoleName" when ordered is null => descending
                    ? query.OrderByDescending(u => u.Role.Name)
                    : query.OrderBy(u => u.Role.Name),
                "RoleName" => descending
                    ? ordered!.ThenByDescending(u => u.Role.Name)
                    : ordered!.ThenBy(u => u.Role.Name),
                nameof(AppUser.UserName) when ordered is null => descending
                    ? query.OrderByDescending(u => u.UserName)
                    : query.OrderBy(u => u.UserName),
                nameof(AppUser.UserName) => descending
                    ? ordered!.ThenByDescending(u => u.UserName)
                    : ordered!.ThenBy(u => u.UserName),
                nameof(AppUser.FullName) when ordered is null => descending
                    ? query.OrderByDescending(u => u.FullName)
                    : query.OrderBy(u => u.FullName),
                nameof(AppUser.FullName) => descending
                    ? ordered!.ThenByDescending(u => u.FullName)
                    : ordered!.ThenBy(u => u.FullName),
                nameof(AppUser.Email) when ordered is null => descending
                    ? query.OrderByDescending(u => u.Email)
                    : query.OrderBy(u => u.Email),
                nameof(AppUser.Email) => descending
                    ? ordered!.ThenByDescending(u => u.Email)
                    : ordered!.ThenBy(u => u.Email),
                nameof(AppUser.IsActive) when ordered is null => descending
                    ? query.OrderByDescending(u => u.IsActive)
                    : query.OrderBy(u => u.IsActive),
                nameof(AppUser.IsActive) => descending
                    ? ordered!.ThenByDescending(u => u.IsActive)
                    : ordered!.ThenBy(u => u.IsActive),
                nameof(AppUser.CreatedAt) when ordered is null => descending
                    ? query.OrderByDescending(u => u.CreatedAt)
                    : query.OrderBy(u => u.CreatedAt),
                nameof(AppUser.CreatedAt) => descending
                    ? ordered!.ThenByDescending(u => u.CreatedAt)
                    : ordered!.ThenBy(u => u.CreatedAt),
                _ => ordered,
            };
        }

        return ordered ?? query.OrderByDescending(u => u.CreatedAt);
    }

    private int? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static List<string>? NormalizePermissionKeys(UpdateUserPermissionsRequest request)
    {
        if (request.HasFullAccess)
        {
            return [];
        }

        if (request.Permissions is null || request.Permissions.Count == 0)
        {
            return [];
        }

        var keys = request.Permissions
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (keys.Any(key => !PermissionService.IsValidPermissionKey(key)))
        {
            return null;
        }

        return keys;
    }

    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public DataTableSearch? Search { get; set; }
        public List<DataTableOrder>? Order { get; set; }
    }

    public class DataTableSearch
    {
        public string? Value { get; set; }
        public bool Regex { get; set; }
    }

    public class DataTableOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; } = "asc";
    }

    public class DataTableResponse<T>
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public List<T> Data { get; set; } = [];
    }

    public class UserTableRow
    {
        public int RowNumber { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool HasFullAccess { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public PersonTitle Title { get; set; }
    }

    public class CreateUserRequest : IValidatableObject
    {
        [Required(ErrorMessage = "نام کاربری الزامی است.")]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "نام کامل الزامی است.")]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "ایمیل الزامی است.")]
        [EmailAddress(ErrorMessage = "فرمت ایمیل معتبر نیست.")]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "شناسه نقش معتبر نیست.")]
        public int RoleId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "شناسه کارمند معتبر نیست.")]
        public int EmployeeId { get; set; }

        public bool IsActive { get; set; } = true;

        public PersonTitle Title { get; set; } = PersonTitle.Mr;

        [Required(ErrorMessage = "رمز عبور الزامی است.")]
        [MinLength(4, ErrorMessage = "رمز عبور باید حداقل ۴ کاراکتر باشد.")]
        [MaxLength(200)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تکرار رمز عبور الزامی است.")]
        [MaxLength(200)]
        public string ConfirmPassword { get; set; } = string.Empty;

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

    public class UpdateUserRequest
    {
        [Required(ErrorMessage = "نام کاربری الزامی است.")]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "نام کامل الزامی است.")]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "ایمیل الزامی است.")]
        [EmailAddress(ErrorMessage = "فرمت ایمیل معتبر نیست.")]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "شناسه نقش معتبر نیست.")]
        public int RoleId { get; set; }

        public bool IsActive { get; set; } = true;

        public PersonTitle Title { get; set; } = PersonTitle.Mr;
    }

    public class ChangePasswordRequest : IValidatableObject
    {
        [Required(ErrorMessage = "رمز عبور الزامی است.")]
        [MinLength(4, ErrorMessage = "رمز عبور باید حداقل ۴ کاراکتر باشد.")]
        [MaxLength(200)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تکرار رمز عبور الزامی است.")]
        [MaxLength(200)]
        public string ConfirmPassword { get; set; } = string.Empty;

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

    public class UpdateUserPermissionsRequest
    {
        public bool HasFullAccess { get; set; } = true;

        public List<string>? Permissions { get; set; }
    }
}
