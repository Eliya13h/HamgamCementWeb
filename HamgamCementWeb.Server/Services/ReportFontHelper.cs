using System.Drawing;
using System.Drawing.Text;
using Stimulsoft.Base;
using Stimulsoft.Report;
using Stimulsoft.Report.Components;
using Stimulsoft.Report.Dictionary;
using Stimulsoft.Report.Export;
using Stimulsoft.Report.Web;

namespace HamgamCementWeb.Server.Services;

public static class ReportFontHelper
{
    /// <summary>نام فونت داخل فایل‌های .mrt و کلید ثبت Stimulsoft برای PDF</summary>
    public const string NotoNastaliqSemiBoldAlias = "Noto Nastaliq Urdu SemiBold";
    public const string NotoNastaliqSemiBoldFileName = "NotoNastaliqUrdu-SemiBold.ttf";
    private const string NotoResourceName = "NotoNastaliqUrduSemiBold";

    private static readonly object FontLock = new();
    private static readonly PrivateFontCollection NotoFontCollection = new();
    private static bool _notoFontLoaded;
    private static string? _notoRealFamilyName;
    private static byte[]? _notoFontBytes;
    private static bool _pdfDefaultsConfigured;

    public static string GetNotoNastaliqFontPath(IWebHostEnvironment env) =>
        Path.Combine(env.ContentRootPath, "Reports", "Fonts", NotoNastaliqSemiBoldFileName);

    /// <summary>
    /// تنظیمات استاتیک PDF (برای همه گزارش‌ها).
    /// </summary>
    public static void ConfigurePdfExportDefaults()
    {
        if (_pdfDefaultsConfigured)
        {
            return;
        }

        // subset کردن فونت نستعلیق گاهی گلیف فارسی را حذف می‌کند → چهارخانه در PDF
        StiOptions.Export.Pdf.ReduceFontFileSize = false;
        _pdfDefaultsConfigured = true;
    }

    /// <summary>
    /// تنظیمات پیش‌فرض Viewer برای Export/Print به PDF با embed اجباری فونت.
    /// </summary>
    public static StiDefaultExportSettings CreateViewerExportSettings()
    {
        ConfigurePdfExportDefaults();

        var settings = new StiDefaultExportSettings();
        settings.ExportToPdf.EmbeddedFonts = true;
        settings.ExportToPdf.StandardPdfFonts = false;
        settings.ExportToPdf.UseUnicode = true;
        return settings;
    }

    public static void RegisterBundledFonts(IWebHostEnvironment env)
    {
        ConfigurePdfExportDefaults();

        var fontPath = GetNotoNastaliqFontPath(env);
        if (!File.Exists(fontPath))
        {
            return;
        }

        try
        {
            // بدون alias — نام داخلی TTF
            StiFontCollection.AddFontFile(fontPath);

            // alias مطابق .mrt — مهم برای lookup در export به PDF
            StiFontCollection.AddFontFile(fontPath, NotoNastaliqSemiBoldAlias);

            var realName = EnsurePrivateFontLoaded(fontPath);
            if (!string.IsNullOrWhiteSpace(realName) &&
                !string.Equals(realName, NotoNastaliqSemiBoldAlias, StringComparison.OrdinalIgnoreCase))
            {
                StiFontCollection.AddFontFile(fontPath, realName);
            }
        }
        catch
        {
            // فونت قبلاً ثبت شده یا فایل معتبر نیست
        }
    }

    /// <summary>
    /// برای مسیر Compile+Render (مثل روزنامچه): Font واقعی GDI از PrivateFontCollection.
    /// </summary>
    public static void ApplyNotoNastaliqSemiBold(StiReport report, IWebHostEnvironment env, string componentName, float sizeInPoints)
    {
        if (report.GetComponentByName(componentName) is not StiText textComponent)
        {
            return;
        }

        var fontPath = GetNotoNastaliqFontPath(env);
        if (!File.Exists(fontPath))
        {
            textComponent.Font = new Font("B Nazanin", sizeInPoints, FontStyle.Regular, GraphicsUnit.Point);
            return;
        }

        try
        {
            RegisterBundledFonts(env);
            var font = CreateFontFromFile(fontPath, sizeInPoints);
            textComponent.Font = font;
            // PDF فونت را با Font.Name جستجو می‌کند
            StiFontCollection.AddFontFile(fontPath, font.Name);
        }
        catch
        {
            textComponent.Font = new Font("B Nazanin", sizeInPoints, FontStyle.Regular, GraphicsUnit.Point);
        }
    }

    /// <summary>
    /// آماده‌سازی برای Viewer + PDF: نام فونت داخل .mrt دست نخورده می‌ماند
    /// («Noto Nastaliq Urdu SemiBold») تا export به PDF همان کلید را در StiFontCollection پیدا کند.
    /// </summary>
    public static void PrepareNotoNastaliqForPdf(
        StiReport report,
        IWebHostEnvironment env,
        string componentName,
        float sizeInPoints)
    {
        _ = componentName;
        _ = sizeInPoints;
        RegisterBundledFonts(env);
        EmbedNotoNastaliqResource(report, env);
    }

    public static void EnsureNotoNastaliqRegistered(IWebHostEnvironment env)
    {
        RegisterBundledFonts(env);
    }

    private static void EmbedNotoNastaliqResource(StiReport report, IWebHostEnvironment env)
    {
        var fontPath = GetNotoNastaliqFontPath(env);
        if (!File.Exists(fontPath))
        {
            return;
        }

        foreach (StiResource existing in report.Dictionary.Resources)
        {
            if (string.Equals(existing.Name, NotoResourceName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        var bytes = GetNotoFontBytes(fontPath);
        var resource = new StiResource(
            NotoResourceName,
            NotoNastaliqSemiBoldAlias,
            false,
            StiResourceType.FontTtf,
            bytes,
            false);

        report.Dictionary.Resources.Add(resource);

        try
        {
            StiFontCollection.AddResourceFont(resource.Name, resource.Content, "ttf", resource.Alias);
            var realName = EnsurePrivateFontLoaded(fontPath);
            if (!string.IsNullOrWhiteSpace(realName) &&
                !string.Equals(realName, resource.Alias, StringComparison.OrdinalIgnoreCase))
            {
                StiFontCollection.AddResourceFont(resource.Name, resource.Content, "ttf", realName);
            }
        }
        catch
        {
            // قبلاً به collection اضافه شده
        }
    }

    private static Font CreateFontFromFile(string fontPath, float sizeInPoints)
    {
        EnsurePrivateFontLoaded(fontPath);
        return new Font(NotoFontCollection.Families[0], sizeInPoints, FontStyle.Regular, GraphicsUnit.Point);
    }

    private static byte[] GetNotoFontBytes(string fontPath)
    {
        lock (FontLock)
        {
            return _notoFontBytes ??= File.ReadAllBytes(fontPath);
        }
    }

    private static string EnsurePrivateFontLoaded(string fontPath)
    {
        lock (FontLock)
        {
            if (!_notoFontLoaded)
            {
                NotoFontCollection.AddFontFile(fontPath);
                _notoFontLoaded = true;
                _notoRealFamilyName = NotoFontCollection.Families.Length > 0
                    ? NotoFontCollection.Families[0].Name
                    : null;
                _notoFontBytes ??= File.ReadAllBytes(fontPath);
            }

            return _notoRealFamilyName ?? string.Empty;
        }
    }
}
