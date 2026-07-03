using System.Globalization;

namespace HamgamCementWeb.Server.Services;

public static class JalaliDateHelper
{
    public static string FormatDate(DateTime date)
    {
        var calendar = new PersianCalendar();
        var year = calendar.GetYear(date);
        var month = calendar.GetMonth(date);
        var day = calendar.GetDayOfMonth(date);
        return $"{year:0000}/{month:00}/{day:00}";
    }
}
