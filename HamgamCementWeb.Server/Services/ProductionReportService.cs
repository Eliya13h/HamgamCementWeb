using System.Data.Common;
using System.Drawing;
using System.Globalization;
using System.Runtime.Versioning;
using Dapper;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using Stimulsoft.Report;
using Stimulsoft.Report.Components;

namespace HamgamCementWeb.Server.Services;

/// <summary>
/// گزارش‌های Stimulsoft تولید:
/// ۱) لیست بازه‌ای اسناد ثبت‌شده (Production.mrt)
/// ۲) سند تفصیلی تک‌بچ با مواد / هزینه / خروجی (ProductionBatch.mrt)
/// </summary>
public interface IProductionReportService
{
    Task<StiReport> BuildListReportAsync(
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);

    Task<StiReport> BuildBatchDetailReportAsync(
        int productionBatchId,
        CancellationToken cancellationToken = default);
}

public class ProductionReportService : IProductionReportService
{
    private const int GeneralSettingsId = 1;
    private const string DefaultZmLogoWebPath = "/zm_logo.jpg";

    private static readonly object ListTemplateLock = new();
    private static StiReport? ListTemplate;
    private static DateTime ListTemplateWriteTimeUtc;

    private static readonly object DetailTemplateLock = new();
    private static StiReport? DetailTemplate;
    private static DateTime DetailTemplateWriteTimeUtc;

    private readonly AppDbContext _db;
    private readonly ISqlConnectionFactory _sql;
    private readonly IWebHostEnvironment _env;

    public ProductionReportService(
        AppDbContext db,
        ISqlConnectionFactory sql,
        IWebHostEnvironment env)
    {
        _db = db;
        _sql = sql;
        _env = env;
    }

    public async Task<StiReport> BuildListReportAsync(
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default)
    {
        var from = dateFrom.Date;
        var to = dateTo.Date;
        if (from > to)
        {
            throw new InvalidOperationException("تاریخ شروع نباید بعد از تاریخ پایان باشد.");
        }

        var settings = await LoadSettingsAsync(cancellationToken);
        var baseSymbol = await LoadBaseSymbolAsync(cancellationToken);
        var rows = await LoadPostedBatchesAsync(from, to, cancellationToken);

        var batches = rows
            .Select((row, index) => new ProductionReportBatchRow
            {
                RowNumber = index + 1,
                ShamsiDate = JalaliDateHelper.FormatDate(row.ProductionDate),
                BatchNumber = row.BatchNumber,
                FormulaName = string.IsNullOrWhiteSpace(row.FormulaName) ? "—" : row.FormulaName!,
                WarehouseName = row.OutputWarehouseName,
                MaterialCost = FormatMoney(row.TotalMaterialCostInBase, baseSymbol),
                ConversionCost = FormatMoney(row.TotalConversionCostInBase, baseSymbol),
                TotalCost = FormatMoney(row.TotalCostInBase, baseSymbol),
                StatusLabel = StatusLabel(row.Status, row.IsPosted),
            })
            .ToList();

        var info = BuildListInfo(settings, from, to, rows, baseSymbol);
        return BuildListReport(info, batches);
    }

    public async Task<StiReport> BuildBatchDetailReportAsync(
        int productionBatchId,
        CancellationToken cancellationToken = default)
    {
        if (productionBatchId <= 0)
        {
            throw new InvalidOperationException("شناسه سند تولید نامعتبر است.");
        }

        var detail = await LoadBatchDetailAsync(productionBatchId, cancellationToken)
            ?? throw new InvalidOperationException("سند تولید یافت نشد.");

        var settings = await LoadSettingsAsync(cancellationToken);
        var baseSymbol = await LoadBaseSymbolAsync(cancellationToken);

        var info = BuildDetailInfo(settings, detail, baseSymbol);
        var batch = MapBatchHeader(detail, baseSymbol);

        // اگر بخشی خالی باشد، یک ردیف راهنما می‌گذاریم تا باند چاپ شود
        var inputLines = detail.InputLines.Count > 0
            ? detail.InputLines
                .Select((line, i) => new ProductionReportInputLine
                {
                    RowNumber = i + 1,
                    ProductName = line.ProductName,
                    WarehouseName = line.WarehouseName,
                    Quantity = FormatQuantity(line.Quantity),
                    UnitName = line.MeaurmentName,
                    MaterialCost = FormatMoney(line.MaterialCostInBase, baseSymbol),
                })
                .ToList()
            : [new ProductionReportInputLine { RowNumber = 1, ProductName = "مادهٔ مصرفی ثبت نشده", WarehouseName = "—", Quantity = "—", UnitName = "—", MaterialCost = "—" }];

        var costLines = detail.CostLines.Count > 0
            ? detail.CostLines
                .Select((line, i) => new ProductionReportCostLine
                {
                    RowNumber = i + 1,
                    CostTypeLabel = CostTypeLabel(line.CostType),
                    Description = string.IsNullOrWhiteSpace(line.Description) ? "—" : line.Description!,
                    Amount = FormatMoney(line.Amount, baseSymbol),
                })
                .ToList()
            : [new ProductionReportCostLine { RowNumber = 1, CostTypeLabel = "هزینه ثبت نشده", Description = "—", Amount = "—" }];

        var outputLines = detail.OutputLines.Count > 0
            ? detail.OutputLines
                .Select((line, i) => new ProductionReportOutputLine
                {
                    RowNumber = i + 1,
                    ProductName = line.ProductName,
                    Quantity = FormatQuantity(line.Quantity),
                    UnitName = line.MeaurmentName,
                    UnitCost = FormatMoney(line.UnitCostInBase, baseSymbol),
                    LotCode = string.IsNullOrWhiteSpace(line.LotCode) ? "—" : line.LotCode!,
                })
                .ToList()
            : [new ProductionReportOutputLine { RowNumber = 1, ProductName = "خروجی ثبت نشده", Quantity = "—", UnitName = "—", UnitCost = "—", LotCode = "—" }];

        return BuildDetailReport(info, batch, inputLines, costLines, outputLines);
    }

    private async Task<GeneralSettings> LoadSettingsAsync(CancellationToken cancellationToken)
    {
        return await _db.GeneralSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == GeneralSettingsId, cancellationToken)
            ?? new GeneralSettings();
    }

    private async Task<string> LoadBaseSymbolAsync(CancellationToken cancellationToken)
    {
        return await _db.Currencies
            .AsNoTracking()
            .Where(c => c.IsBaseCurrency && c.IsDeleted != true)
            .Select(c => c.Symbol)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
    }

    /// <summary>فقط اسناد ثبت‌شده در بازهٔ تاریخ تولید.</summary>
    private async Task<List<ProductionReportListSourceRow>> LoadPostedBatchesAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<ProductionReportListSourceRow>(
            new CommandDefinition(
                """
                SELECT
                    b.ProductionBatchID AS ProductionBatchId,
                    b.BatchNumber,
                    b.ProductionDate,
                    f.Name AS FormulaName,
                    w.Name AS OutputWarehouseName,
                    CAST(b.Status AS int) AS Status,
                    CAST(CASE WHEN b.IsPosted = 1 THEN 1 ELSE 0 END AS bit) AS IsPosted,
                    b.TotalMaterialCostInBase,
                    b.TotalConversionCostInBase,
                    b.TotalCostInBase
                FROM dbo.ProductionBatches b
                INNER JOIN dbo.Warehouses w ON w.WarehouseID = b.OutputWarehouseId
                LEFT JOIN dbo.ProductionFormulas f ON f.ProductionFormulaID = b.ProductionFormulaId
                WHERE b.IsDeleted = 0
                  AND b.IsPosted = 1
                  AND CAST(b.ProductionDate AS date) >= @From
                  AND CAST(b.ProductionDate AS date) <= @To
                ORDER BY b.ProductionDate DESC, b.ProductionBatchID DESC
                """,
                new { From = from, To = to },
                cancellationToken: cancellationToken));

        return rows.AsList();
    }

    private async Task<ProductionReportDetailSource?> LoadBatchDetailAsync(
        int productionBatchId,
        CancellationToken cancellationToken)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        var header = await connection.QuerySingleOrDefaultAsync<ProductionReportDetailHeaderRow>(
            new CommandDefinition(
                """
                SELECT
                    b.ProductionBatchID AS ProductionBatchId,
                    b.BatchNumber,
                    b.ProductionDate,
                    f.Name AS FormulaName,
                    CASE
                        WHEN b.ProductionPlanId IS NULL THEN NULL
                        ELSE CONCAT(ISNULL(pr.Name, N''), N' — ', CONVERT(varchar(10), p.PlanDate, 23))
                    END AS PlanLabel,
                    w.Name AS OutputWarehouseName,
                    CAST(b.Status AS int) AS Status,
                    CAST(CASE WHEN b.IsPosted = 1 THEN 1 ELSE 0 END AS bit) AS IsPosted,
                    b.TotalMaterialCostInBase,
                    b.TotalConversionCostInBase,
                    b.TotalCostInBase,
                    b.JournalEntryId,
                    je.EntryNumber AS JournalEntryNumber,
                    b.Description
                FROM dbo.ProductionBatches b
                INNER JOIN dbo.Warehouses w ON w.WarehouseID = b.OutputWarehouseId
                LEFT JOIN dbo.ProductionFormulas f ON f.ProductionFormulaID = b.ProductionFormulaId
                LEFT JOIN dbo.ProductionPlans p ON p.ProductionPlanID = b.ProductionPlanId
                LEFT JOIN dbo.Products pr ON pr.ProductID = p.ProductId
                LEFT JOIN dbo.JournalEntries je ON je.JournalEntryID = b.JournalEntryId AND je.IsDeleted = 0
                WHERE b.ProductionBatchID = @Id AND b.IsDeleted = 0
                """,
                new { Id = productionBatchId },
                cancellationToken: cancellationToken));

        if (header is null)
        {
            return null;
        }

        var inputLines = (await connection.QueryAsync<ProductionReportInputSourceRow>(
            new CommandDefinition(
                """
                SELECT
                    p.Name AS ProductName,
                    wh.Name AS WarehouseName,
                    i.Quantity,
                    m.Name AS MeaurmentName,
                    i.MaterialCostInBase
                FROM dbo.ProductionInputLines i
                INNER JOIN dbo.Warehouses wh ON wh.WarehouseID = i.WarehouseId
                INNER JOIN dbo.Products p ON p.ProductID = i.ProductId
                INNER JOIN dbo.Meaurments m ON m.MeaurmentID = i.MeaurmentId
                WHERE i.ProductionBatchId = @Id AND i.IsDeleted = 0
                ORDER BY i.ProductionInputLineID
                """,
                new { Id = productionBatchId },
                cancellationToken: cancellationToken))).AsList();

        var costLines = (await connection.QueryAsync<ProductionReportCostSourceRow>(
            new CommandDefinition(
                """
                SELECT
                    CAST(c.CostType AS int) AS CostType,
                    c.Description,
                    c.Amount
                FROM dbo.ProductionBatchCostLines c
                WHERE c.ProductionBatchId = @Id AND c.IsDeleted = 0
                ORDER BY c.ProductionBatchCostLineID
                """,
                new { Id = productionBatchId },
                cancellationToken: cancellationToken))).AsList();

        var outputLines = (await connection.QueryAsync<ProductionReportOutputSourceRow>(
            new CommandDefinition(
                """
                SELECT
                    p.Name AS ProductName,
                    o.Quantity,
                    m.Name AS MeaurmentName,
                    o.UnitCostInBase,
                    l.LotCode
                FROM dbo.ProductionOutputLines o
                INNER JOIN dbo.Products p ON p.ProductID = o.ProductId
                INNER JOIN dbo.Meaurments m ON m.MeaurmentID = o.MeaurmentId
                LEFT JOIN dbo.InventoryLots l ON l.InventoryLotID = o.InventoryLotId AND l.IsDeleted = 0
                WHERE o.ProductionBatchId = @Id AND o.IsDeleted = 0
                ORDER BY o.ProductionOutputLineID
                """,
                new { Id = productionBatchId },
                cancellationToken: cancellationToken))).AsList();

        return new ProductionReportDetailSource
        {
            Header = header,
            InputLines = inputLines,
            CostLines = costLines,
            OutputLines = outputLines,
        };
    }

    private ProductionReportInfo BuildListInfo(
        GeneralSettings settings,
        DateTime from,
        DateTime to,
        IReadOnlyList<ProductionReportListSourceRow> rows,
        string baseSymbol)
    {
        var material = rows.Sum(r => r.TotalMaterialCostInBase);
        var conversion = rows.Sum(r => r.TotalConversionCostInBase);
        var total = rows.Sum(r => r.TotalCostInBase);

        return new ProductionReportInfo
        {
            PersianCompanyName = settings.PersianCompanyName,
            EnglishCompanyName = settings.EnglishCompanyName,
            ZmLogo = ResolveLogoPath(string.IsNullOrWhiteSpace(settings.ZmLogoPath) ? DefaultZmLogoWebPath : settings.ZmLogoPath),
            CompanyLogo = ResolveLogoPath(settings.CompanyLogoPath),
            PrintDate = JalaliDateHelper.FormatDate(DateTime.Now),
            ReportTitle = "گزارش تولیدات",
            ReportRangeDate = $"از {JalaliDateHelper.FormatDate(from)} تا {JalaliDateHelper.FormatDate(to)} — {rows.Count} سند",
            TotalMaterialCost = FormatMoney(material, baseSymbol),
            TotalConversionCost = FormatMoney(conversion, baseSymbol),
            GrandTotal = FormatMoney(total, baseSymbol),
            RowCount = rows.Count.ToString(CultureInfo.InvariantCulture),
        };
    }

    private ProductionReportInfo BuildDetailInfo(
        GeneralSettings settings,
        ProductionReportDetailSource detail,
        string baseSymbol)
    {
        var h = detail.Header;
        return new ProductionReportInfo
        {
            PersianCompanyName = settings.PersianCompanyName,
            EnglishCompanyName = settings.EnglishCompanyName,
            ZmLogo = ResolveLogoPath(string.IsNullOrWhiteSpace(settings.ZmLogoPath) ? DefaultZmLogoWebPath : settings.ZmLogoPath),
            CompanyLogo = ResolveLogoPath(settings.CompanyLogoPath),
            PrintDate = JalaliDateHelper.FormatDate(DateTime.Now),
            ReportTitle = "سند تولید",
            ReportRangeDate = $"شماره {h.BatchNumber} — {JalaliDateHelper.FormatDate(h.ProductionDate)}",
            TotalMaterialCost = FormatMoney(h.TotalMaterialCostInBase, baseSymbol),
            TotalConversionCost = FormatMoney(h.TotalConversionCostInBase, baseSymbol),
            GrandTotal = FormatMoney(h.TotalCostInBase, baseSymbol),
            RowCount = "1",
        };
    }

    private static ProductionReportBatchHeader MapBatchHeader(
        ProductionReportDetailSource detail,
        string baseSymbol)
    {
        var h = detail.Header;
        return new ProductionReportBatchHeader
        {
            BatchNumber = h.BatchNumber,
            ShamsiDate = JalaliDateHelper.FormatDate(h.ProductionDate),
            FormulaName = string.IsNullOrWhiteSpace(h.FormulaName) ? "—" : h.FormulaName!,
            WarehouseName = h.OutputWarehouseName,
            PlanLabel = string.IsNullOrWhiteSpace(h.PlanLabel) ? "—" : h.PlanLabel!,
            Description = string.IsNullOrWhiteSpace(h.Description) ? "—" : h.Description!,
            MaterialCost = FormatMoney(h.TotalMaterialCostInBase, baseSymbol),
            ConversionCost = FormatMoney(h.TotalConversionCostInBase, baseSymbol),
            TotalCost = FormatMoney(h.TotalCostInBase, baseSymbol),
            StatusLabel = StatusLabel(h.Status, h.IsPosted),
            JournalEntryNumber = string.IsNullOrWhiteSpace(h.JournalEntryNumber) ? "—" : h.JournalEntryNumber!,
        };
    }

    private StiReport BuildListReport(ProductionReportInfo info, IReadOnlyList<ProductionReportBatchRow> batches)
    {
        var report = CreateFromTemplate(
            "Production.mrt",
            "فایل گزارش تولیدات یافت نشد.",
            ListTemplateLock,
            ref ListTemplate,
            ref ListTemplateWriteTimeUtc);

        report.RegBusinessObject("Info", info);
        report.RegBusinessObject("Batches", batches);
        report.Dictionary.Synchronize();
        ApplyReportImages(report, info);
        report.CalculationMode = StiCalculationMode.Interpretation;
        ReportFontHelper.ApplyNotoNastaliqSemiBold(report, _env, "Text1", 14F);
        report.Render(false);
        return report;
    }

    private StiReport BuildDetailReport(
        ProductionReportInfo info,
        ProductionReportBatchHeader batch,
        IReadOnlyList<ProductionReportInputLine> inputLines,
        IReadOnlyList<ProductionReportCostLine> costLines,
        IReadOnlyList<ProductionReportOutputLine> outputLines)
    {
        var report = CreateFromTemplate(
            "ProductionBatch.mrt",
            "فایل گزارش تفصیلی تولید یافت نشد.",
            DetailTemplateLock,
            ref DetailTemplate,
            ref DetailTemplateWriteTimeUtc);

        report.RegBusinessObject("Info", info);
        // لیست تک‌عضوی برای DataBand هدر سند
        report.RegBusinessObject("Batch", new List<ProductionReportBatchHeader> { batch });
        report.RegBusinessObject("InputLines", inputLines);
        report.RegBusinessObject("CostLines", costLines);
        report.RegBusinessObject("OutputLines", outputLines);
        report.Dictionary.Synchronize();
        ApplyReportImages(report, info);
        report.CalculationMode = StiCalculationMode.Interpretation;
        ReportFontHelper.ApplyNotoNastaliqSemiBold(report, _env, "Text1", 14F);
        report.Render(false);
        return report;
    }

    private StiReport CreateFromTemplate(
        string fileName,
        string notFoundMessage,
        object lockObj,
        ref StiReport? cached,
        ref DateTime cachedWriteTimeUtc)
    {
        var reportPath = Path.Combine(_env.ContentRootPath, "Reports", fileName);
        if (!File.Exists(reportPath))
        {
            throw new FileNotFoundException(notFoundMessage, reportPath);
        }

        var writeTimeUtc = File.GetLastWriteTimeUtc(reportPath);
        lock (lockObj)
        {
            if (cached is null || cachedWriteTimeUtc != writeTimeUtc)
            {
                var template = new StiReport();
                template.Load(reportPath);
                template.CalculationMode = StiCalculationMode.Interpretation;
                cached = template;
                cachedWriteTimeUtc = writeTimeUtc;
            }

            return (StiReport)cached.Clone();
        }
    }

    private static void ApplyReportImages(StiReport report, ProductionReportInfo info)
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

    private static string StatusLabel(int status, bool isPosted)
    {
        if (isPosted || status == (int)ProductionBatchStatus.Posted)
        {
            return "ثبت‌شده";
        }

        return "پیش‌نویس";
    }

    private static string CostTypeLabel(int costType) => ((ProductionCostType)costType) switch
    {
        ProductionCostType.DirectWage => "هزینه تولید مستقیم",
        ProductionCostType.Overhead => "هزینه تولید غیر مستقیم",
        ProductionCostType.Ancillary => "هزینه جانبی",
        ProductionCostType.Fixed => "هزینه ثابت",
        ProductionCostType.ProductionBurden => "سربار تولید",
        _ => costType.ToString(CultureInfo.InvariantCulture),
    };

    private static string FormatQuantity(decimal value)
        => value.ToString("#,##0.####", CultureInfo.InvariantCulture);

    private static string FormatMoney(decimal amount, string symbol)
    {
        var formatted = amount.ToString("#,##0.##", CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(symbol) ? formatted : $"{formatted} {symbol}";
    }

    private sealed class ProductionReportListSourceRow
    {
        public int ProductionBatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ProductionDate { get; set; }
        public string? FormulaName { get; set; }
        public string OutputWarehouseName { get; set; } = string.Empty;
        public int Status { get; set; }
        public bool IsPosted { get; set; }
        public decimal TotalMaterialCostInBase { get; set; }
        public decimal TotalConversionCostInBase { get; set; }
        public decimal TotalCostInBase { get; set; }
    }

    private sealed class ProductionReportDetailHeaderRow
    {
        public int ProductionBatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ProductionDate { get; set; }
        public string? FormulaName { get; set; }
        public string? PlanLabel { get; set; }
        public string OutputWarehouseName { get; set; } = string.Empty;
        public int Status { get; set; }
        public bool IsPosted { get; set; }
        public decimal TotalMaterialCostInBase { get; set; }
        public decimal TotalConversionCostInBase { get; set; }
        public decimal TotalCostInBase { get; set; }
        public int? JournalEntryId { get; set; }
        public string? JournalEntryNumber { get; set; }
        public string? Description { get; set; }
    }

    private sealed class ProductionReportInputSourceRow
    {
        public string ProductName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string MeaurmentName { get; set; } = string.Empty;
        public decimal MaterialCostInBase { get; set; }
    }

    private sealed class ProductionReportCostSourceRow
    {
        public int CostType { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
    }

    private sealed class ProductionReportOutputSourceRow
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string MeaurmentName { get; set; } = string.Empty;
        public decimal UnitCostInBase { get; set; }
        public string? LotCode { get; set; }
    }

    private sealed class ProductionReportDetailSource
    {
        public ProductionReportDetailHeaderRow Header { get; set; } = new();
        public List<ProductionReportInputSourceRow> InputLines { get; set; } = [];
        public List<ProductionReportCostSourceRow> CostLines { get; set; } = [];
        public List<ProductionReportOutputSourceRow> OutputLines { get; set; } = [];
    }
}

/// <summary>هدر مشترک گزارش تولید (Info در MRT).</summary>
public class ProductionReportInfo
{
    public string CompanyLogo { get; set; } = string.Empty;
    public string EnglishCompanyName { get; set; } = string.Empty;
    public string PersianCompanyName { get; set; } = string.Empty;
    public string ZmLogo { get; set; } = string.Empty;
    public string PrintDate { get; set; } = string.Empty;
    public string ReportTitle { get; set; } = string.Empty;
    public string ReportRangeDate { get; set; } = string.Empty;
    public string TotalMaterialCost { get; set; } = string.Empty;
    public string TotalConversionCost { get; set; } = string.Empty;
    public string GrandTotal { get; set; } = string.Empty;
    public string RowCount { get; set; } = string.Empty;
}

/// <summary>ردیف لیست بازه‌ای (Batches در Production.mrt).</summary>
public class ProductionReportBatchRow
{
    public int RowNumber { get; set; }
    public string ShamsiDate { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public string FormulaName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string MaterialCost { get; set; } = string.Empty;
    public string ConversionCost { get; set; } = string.Empty;
    public string TotalCost { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
}

/// <summary>هدر سند تفصیلی (Batch در ProductionBatch.mrt).</summary>
public class ProductionReportBatchHeader
{
    public string BatchNumber { get; set; } = string.Empty;
    public string ShamsiDate { get; set; } = string.Empty;
    public string FormulaName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string PlanLabel { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MaterialCost { get; set; } = string.Empty;
    public string ConversionCost { get; set; } = string.Empty;
    public string TotalCost { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string JournalEntryNumber { get; set; } = string.Empty;
}

public class ProductionReportInputLine
{
    public int RowNumber { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string MaterialCost { get; set; } = string.Empty;
}

public class ProductionReportCostLine
{
    public int RowNumber { get; set; }
    public string CostTypeLabel { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
}

public class ProductionReportOutputLine
{
    public int RowNumber { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string UnitCost { get; set; } = string.Empty;
    public string LotCode { get; set; } = string.Empty;
}
