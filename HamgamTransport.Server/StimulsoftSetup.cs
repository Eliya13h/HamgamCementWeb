using HamgamTransport.Server.Services;

namespace HamgamTransport.Server;

public static class StimulsoftSetup
{
    public static void RegisterReportFonts(IWebHostEnvironment env)
    {
        ReportFontHelper.ConfigurePdfExportDefaults();
        ReportFontHelper.RegisterBundledFonts(env);
    }
}
