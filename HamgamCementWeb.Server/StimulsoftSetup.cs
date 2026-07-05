using HamgamCementWeb.Server.Services;

namespace HamgamCementWeb.Server;

public static class StimulsoftSetup
{
    public static void RegisterReportFonts(IWebHostEnvironment env)
    {
        ReportFontHelper.RegisterBundledFonts(env);
    }
}
