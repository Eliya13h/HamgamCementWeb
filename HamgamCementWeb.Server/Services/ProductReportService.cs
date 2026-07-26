using System.Drawing;
using System.Globalization;
using System.Runtime.Versioning;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using Stimulsoft.Report;
using Stimulsoft.Report.Components;

namespace HamgamCementWeb.Server.Services;

public interface IProductReportService
{
    Task<StiReport> BuildProductsReportAsync(
        int? categoryId = null,
        bool? activeOnly = null,
        bool belowMinStockOnly = false,
        CancellationToken cancellationToken = default);
}

public class ProductReportService : IProductReportService
{
    private const int GeneralSettingsId = 1;
    private const string DefaultZmLogoWebPath = "/zm_logo.jpg";

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IProductPurchasePriceHintService _purchasePriceHints;

    public ProductReportService(
        AppDbContext db,
        IWebHostEnvironment env,
        IProductPurchasePriceHintService purchasePriceHints)
    {
        _db = db;
        _env = env;
        _purchasePriceHints = purchasePriceHints;
    }

    public async Task<StiReport> BuildProductsReportAsync(
        int? categoryId = null,
        bool? activeOnly = null,
        bool belowMinStockOnly = false,
        CancellationToken cancellationToken = default)
    {
        var settings = await _db.GeneralSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == GeneralSettingsId, cancellationToken)
            ?? new GeneralSettings();

        var query = _db.Products
            .AsNoTracking()
            .Where(p => p.IsDeleted != true);

        if (activeOnly == true)
        {
            query = query.Where(p => p.IsActive == true);
        }
        else if (activeOnly == false)
        {
            query = query.Where(p => p.IsActive != true);
        }

        if (categoryId is > 0)
        {
            query = query.Where(p =>
                p.ProductCategories.Any(pc =>
                    pc.IsDeleted != true && pc.CategoryId == categoryId.Value));
        }

        var rawRows = await query
            .OrderBy(p => p.Code)
            .ThenBy(p => p.Name)
            .Select(p => new
            {
                p.ProductID,
                p.Code,
                p.Name,
                UnitName = p.BaseMeaurment.Name,
                p.DefaultSalePrice,
                p.MinStockQuantity,
                TotalStockQuantity = _db.InventoryStocks
                    .Where(s => s.ProductId == p.ProductID && s.IsDeleted != true)
                    .Sum(s => (decimal?)s.QuantityInBase) ?? 0m,
                Categories = p.ProductCategories
                    .Where(pc => pc.IsDeleted != true)
                    .Select(pc => pc.Category.Name)
                    .ToList(),
                IsActive = p.IsActive == true,
            })
            .ToListAsync(cancellationToken);

        var hints = await _purchasePriceHints.GetHintsAsync(
            rawRows.Select(r => r.ProductID),
            cancellationToken: cancellationToken);

        var rows = rawRows
            .Select(r => new ProductReportSourceRow
            {
                Code = r.Code,
                Name = r.Name,
                UnitName = r.UnitName,
                DefaultPurchasePrice = hints.TryGetValue(r.ProductID, out var hint)
                    ? hint.UnitCostInBase ?? 0m
                    : 0m,
                DefaultSalePrice = r.DefaultSalePrice,
                MinStockQuantity = r.MinStockQuantity,
                TotalStockQuantity = r.TotalStockQuantity,
                Categories = string.Join("، ", r.Categories.OrderBy(n => n)),
                IsActive = r.IsActive,
            })
            .Where(r => !belowMinStockOnly || (r.MinStockQuantity > 0 && r.TotalStockQuantity < r.MinStockQuantity))
            .ToList();

        var products = rows
            .Select((row, index) => MapProductRow(row, index + 1))
            .ToList();

        string? categoryName = null;
        if (categoryId is > 0)
        {
            categoryName = await _db.Categories
                .AsNoTracking()
                .Where(c => c.CategoryID == categoryId.Value && c.IsDeleted != true)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var info = BuildInfo(settings, products.Count, categoryName, activeOnly, belowMinStockOnly);
        return BuildReport(info, products);
    }

    private JournalReportInfo BuildInfo(
        GeneralSettings settings,
        int productCount,
        string? categoryName,
        bool? activeOnly,
        bool belowMinStockOnly)
    {
        var zmLogoWebPath = string.IsNullOrWhiteSpace(settings.ZmLogoPath)
            ? DefaultZmLogoWebPath
            : settings.ZmLogoPath;

        var filterParts = new List<string>();
        if (activeOnly == true)
        {
            filterParts.Add("فقط فعال");
        }
        else if (activeOnly == false)
        {
            filterParts.Add("فقط غیرفعال");
        }

        if (belowMinStockOnly)
        {
            filterParts.Add("زیر حداقل موجودی");
        }

        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            filterParts.Add($"دسته: {categoryName}");
        }

        filterParts.Add($"{productCount.ToString(CultureInfo.InvariantCulture)} قلم");

        return new JournalReportInfo
        {
            PersianCompanyName = settings.PersianCompanyName,
            EnglishCompanyName = settings.EnglishCompanyName,
            ZmLogo = ResolveLogoPath(zmLogoWebPath),
            CompanyLogo = ResolveLogoPath(settings.CompanyLogoPath),
            PrintDate = JalaliDateHelper.FormatDate(DateTime.Now),
            ReportTitle = "گزارش جامع محصولات",
            ReportRangeDate = string.Join(" — ", filterParts),
        };
    }

    private static ProductReportRow MapProductRow(ProductReportSourceRow row, int rowNumber)
    {
        var isBelowMin = row.MinStockQuantity > 0 && row.TotalStockQuantity < row.MinStockQuantity;
        var status = !row.IsActive
            ? "غیرفعال"
            : isBelowMin
                ? "کمبود"
                : "فعال";

        return new ProductReportRow
        {
            RowNumber = rowNumber,
            Code = row.Code?.Trim() ?? string.Empty,
            Name = row.Name?.Trim() ?? string.Empty,
            Categories = string.IsNullOrWhiteSpace(row.Categories) ? "—" : row.Categories,
            UnitName = string.IsNullOrWhiteSpace(row.UnitName) ? "—" : row.UnitName,
            StockQuantity = FormatQuantity(row.TotalStockQuantity),
            MinStockQuantity = FormatQuantity(row.MinStockQuantity),
            PurchasePrice = FormatMoney(row.DefaultPurchasePrice),
            SalePrice = FormatMoney(row.DefaultSalePrice),
            Status = status,
        };
    }

    private StiReport BuildReport(JournalReportInfo info, IReadOnlyList<ProductReportRow> products)
    {
        var reportPath = Path.Combine(_env.ContentRootPath, "Reports", "Products.mrt");
        if (!File.Exists(reportPath))
        {
            throw new FileNotFoundException("فایل گزارش محصولات یافت نشد.", reportPath);
        }

        var report = new StiReport();
        report.Load(reportPath);
        report.RegBusinessObject("Info", info);
        report.RegBusinessObject("Products", products);
        report.Dictionary.Synchronize();
        ApplyReportImages(report, info);
        report.Compile();
        ReportFontHelper.ApplyNotoNastaliqSemiBold(report, _env, "Text1", 14F);
        report.Render();
        return report;
    }

    private static void ApplyReportImages(StiReport report, JournalReportInfo info)
    {
        // در mrt نام Imageها با فیلدهای Info جابجا شده: CompanyLogo ← ZmLogo ، ZmLogo ← CompanyLogo
        SetReportImage(report, "CompanyLogo", info.ZmLogo);
        SetReportImage(report, "ZmLogo", info.CompanyLogo);
    }

    [SupportedOSPlatform("windows")]
    private static void SetReportImage(StiReport report, string componentName, string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        if (report.GetComponentByName(componentName) is not StiImage image)
        {
            return;
        }

        using var stream = new MemoryStream(File.ReadAllBytes(path));
        image.Image = Image.FromStream(stream);
    }

    private string ResolveLogoPath(string? webPath)
    {
        if (string.IsNullOrWhiteSpace(webPath))
        {
            return string.Empty;
        }

        var relativePath = webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fileName = Path.GetFileName(relativePath);
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(_env.WebRootPath))
        {
            candidates.Add(Path.Combine(_env.WebRootPath, relativePath));
            candidates.Add(Path.Combine(_env.WebRootPath, fileName));
        }

        candidates.Add(Path.GetFullPath(Path.Combine(
            _env.ContentRootPath,
            "..",
            "hamgamcementweb.client",
            "public",
            fileName)));

        return candidates.FirstOrDefault(File.Exists) ?? string.Empty;
    }

    private static string FormatQuantity(decimal value)
    {
        return value.ToString("#,##0.####", CultureInfo.InvariantCulture);
    }

    private static string FormatMoney(decimal amount)
    {
        return amount.ToString("#,##0.##", CultureInfo.InvariantCulture);
    }

    private sealed class ProductReportSourceRow
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public decimal DefaultPurchasePrice { get; set; }
        public decimal DefaultSalePrice { get; set; }
        public decimal MinStockQuantity { get; set; }
        public decimal TotalStockQuantity { get; set; }
        public string Categories { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}

public class ProductReportRow
{
    public int RowNumber { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Categories { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string StockQuantity { get; set; } = string.Empty;
    public string MinStockQuantity { get; set; } = string.Empty;
    public string PurchasePrice { get; set; } = string.Empty;
    public string SalePrice { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
