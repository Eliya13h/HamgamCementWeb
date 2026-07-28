using System.ComponentModel.DataAnnotations;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Settings;

[ApiController]
[Route("api/settings/general")]
[Authorize]
public class GeneralSettingsController : ControllerBase
{
    private const string DefaultZmLogoPath = "/zm_logo.jpg";
    private const long MaxLogoSizeBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> AllowedLogoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp",
    };

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public GeneralSettingsController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    [HasPermission("settings.view")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        return Ok(MapToResponse(settings));
    }

    [HttpPut]
    [HasPermission("settings.edit")]
    public async Task<IActionResult> Update(
        [FromBody] SaveGeneralSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var settings = await GetOrCreateSettingsAsync(cancellationToken);

        settings.PersianCompanyName = request.PersianCompanyName.Trim();
        settings.EnglishCompanyName = request.EnglishCompanyName.Trim();
        settings.ZmLogoPath = DefaultZmLogoPath;
        settings.CompanyLogoPath = string.IsNullOrWhiteSpace(request.CompanyLogoPath)
            ? string.Empty
            : request.CompanyLogoPath.Trim();
        settings.CompanyAddress = request.CompanyAddress?.Trim() ?? string.Empty;
        settings.CompanyPhoneNumber1 = request.CompanyPhoneNumber1.Trim();
        settings.CompanyPhoneNumber2 = request.CompanyPhoneNumber2?.Trim() ?? string.Empty;
        settings.CompanyPhoneNumber3 = request.CompanyPhoneNumber3?.Trim() ?? string.Empty;
        settings.CompanyEmail = request.CompanyEmail.Trim();
        settings.CompanySite = request.CompanySite?.Trim() ?? string.Empty;
        settings.DefaultTaxPercent = request.DefaultTaxPercent;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "تنظیمات عمومی با موفقیت ذخیره شد.",
            settings = MapToResponse(settings),
        });
    }

    [HttpPost("company-logo")]
    [HasPermission("settings.edit")]
    [RequestSizeLimit(MaxLogoSizeBytes)]
    public async Task<IActionResult> UploadCompanyLogo(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "فایل لوگو انتخاب نشده است." });
        }

        if (file.Length > MaxLogoSizeBytes)
        {
            return BadRequest(new { message = "حجم فایل لوگو نباید بیشتر از ۲ مگابایت باشد." });
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedLogoExtensions.Contains(extension))
        {
            return BadRequest(new { message = "فرمت فایل لوگو مجاز نیست. (jpg, png, webp)" });
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "company-logo");
        Directory.CreateDirectory(uploadsDir);

        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        DeleteExistingLogoFile(settings.CompanyLogoPath);

        var fileName = $"company-logo{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(uploadsDir, fileName);

        await using (var stream = System.IO.File.Create(physicalPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        settings.CompanyLogoPath = $"/uploads/company-logo/{fileName}";
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "لوگوی سازمان با موفقیت آپلود شد.",
            companyLogoPath = settings.CompanyLogoPath,
        });
    }

    private async Task<GeneralSettings> GetOrCreateSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.GeneralSettings
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is not null)
        {
            if (string.IsNullOrWhiteSpace(settings.ZmLogoPath))
            {
                settings.ZmLogoPath = DefaultZmLogoPath;
            }

            return settings;
        }

        settings = new GeneralSettings
        {
            ZmLogoPath = DefaultZmLogoPath,
        };

        _db.GeneralSettings.Add(settings);
        await _db.SaveChangesAsync(cancellationToken);

        return settings;
    }

    private void DeleteExistingLogoFile(string? webPath)
    {
        if (string.IsNullOrWhiteSpace(webPath))
        {
            return;
        }

        var relativePath = webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.Combine(_env.WebRootPath, relativePath);

        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }
    }

    private static object MapToResponse(GeneralSettings settings) => new
    {
        settings.Id,
        settings.PersianCompanyName,
        settings.EnglishCompanyName,
        zmLogoPath = string.IsNullOrWhiteSpace(settings.ZmLogoPath) ? DefaultZmLogoPath : settings.ZmLogoPath,
        settings.CompanyLogoPath,
        settings.CompanyAddress,
        settings.CompanyPhoneNumber1,
        settings.CompanyPhoneNumber2,
        settings.CompanyPhoneNumber3,
        settings.CompanyEmail,
        settings.CompanySite,
        settings.DefaultTaxPercent,
    };

    public class SaveGeneralSettingsRequest
    {
        [Required(ErrorMessage = "نام فارسی شرکت الزامی است.")]
        [MaxLength(300)]
        public string PersianCompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "نام انگلیسی شرکت الزامی است.")]
        [MaxLength(300)]
        public string EnglishCompanyName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? CompanyLogoPath { get; set; }

        [MaxLength(500)]
        public string? CompanyAddress { get; set; }

        [Required(ErrorMessage = "تلفن ۱ الزامی است.")]
        [MaxLength(50)]
        public string CompanyPhoneNumber1 { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? CompanyPhoneNumber2 { get; set; }

        [MaxLength(50)]
        public string? CompanyPhoneNumber3 { get; set; }

        [Required(ErrorMessage = "ایمیل الزامی است.")]
        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "ایمیل نامعتبر است.")]
        public string CompanyEmail { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? CompanySite { get; set; }

        [Range(0, 100)]
        public decimal DefaultTaxPercent { get; set; }
    }
}
