using System.Drawing;
using System.Drawing.Text;
using Stimulsoft.Base;
using Stimulsoft.Report;
using Stimulsoft.Report.Components;

namespace HamgamCementWeb.Server.Services;

public static class ReportFontHelper
{
    public const string NotoNastaliqSemiBoldAlias = "Noto Nastaliq Urdu SemiBold";
    public const string NotoNastaliqSemiBoldFileName = "NotoNastaliqUrdu-SemiBold.ttf";

    private static readonly PrivateFontCollection NotoFontCollection = new();
    private static bool _notoFontLoaded;

    public static string GetNotoNastaliqFontPath(IWebHostEnvironment env) =>
        Path.Combine(env.ContentRootPath, "Reports", "Fonts", NotoNastaliqSemiBoldFileName);

    public static void RegisterBundledFonts(IWebHostEnvironment env)
    {
        var fontPath = GetNotoNastaliqFontPath(env);
        if (!File.Exists(fontPath))
        {
            return;
        }

        try
        {
            StiFontCollection.AddFontFile(fontPath, NotoNastaliqSemiBoldAlias);
        }
        catch
        {
            // فونت قبلاً ثبت شده یا فایل معتبر نیست
        }
    }

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
            StiFontCollection.AddFontFile(fontPath, NotoNastaliqSemiBoldAlias);
            textComponent.Font = CreateFontFromFile(fontPath, sizeInPoints);
        }
        catch
        {
            textComponent.Font = new Font("B Nazanin", sizeInPoints, FontStyle.Regular, GraphicsUnit.Point);
        }
    }

    private static Font CreateFontFromFile(string fontPath, float sizeInPoints)
    {
        if (!_notoFontLoaded)
        {
            NotoFontCollection.AddFontFile(fontPath);
            _notoFontLoaded = true;
        }

        return new Font(NotoFontCollection.Families[0], sizeInPoints, FontStyle.Regular, GraphicsUnit.Point);
    }
}
