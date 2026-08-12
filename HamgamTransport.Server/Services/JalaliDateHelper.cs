using System.Globalization;

namespace HamgamTransport.Server.Services;

public static class JalaliDateHelper
{
    // نام ماه‌های تقویم شمسی افغانستان (حمل … حوت)
    public static readonly string[] AfghanMonthNames =
    [
        "حمل", "ثور", "جوزا", "سرطان", "اسد", "سنبله",
        "میزان", "عقرب", "قوس", "جدی", "دلو", "حوت",
    ];

    public static string FormatDate(DateTime date)
    {
        var calendar = new PersianCalendar();
        var year = calendar.GetYear(date);
        var month = calendar.GetMonth(date);
        var day = calendar.GetDayOfMonth(date);
        return $"{year:0000}/{month:00}/{day:00}";
    }

    // نمایش تاریخ با نام ماه افغانی؛ مثلاً ۲۲ سرطان ۱۴۰۴
    public static string FormatDateWithMonthName(DateTime date)
    {
        var calendar = new PersianCalendar();
        var year = calendar.GetYear(date);
        var month = calendar.GetMonth(date);
        var day = calendar.GetDayOfMonth(date);
        return $"{day} {AfghanMonthNames[month - 1]} {year}";
    }

    public static int GetSolarYear(DateTime date)
    {
        return new PersianCalendar().GetYear(date);
    }

    // ماه شمسی متناظر با تاریخ میلادی
    public static int GetSolarMonth(DateTime date)
    {
        return new PersianCalendar().GetMonth(date);
    }

    // بازه میلادی یک سال شمسی: ۱ فروردین تا آخرین روز اسفند
    public static (DateTime Start, DateTime End) GetSolarYearRange(int solarYear)
    {
        var calendar = new PersianCalendar();
        var start = calendar.ToDateTime(solarYear, 1, 1, 0, 0, 0, 0);
        var daysInLastMonth = calendar.GetDaysInMonth(solarYear, 12);
        var end = calendar.ToDateTime(solarYear, 12, daysInLastMonth, 23, 59, 59, 999);
        return (start, end);
    }
}
