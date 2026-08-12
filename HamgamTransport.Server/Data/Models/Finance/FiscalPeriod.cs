using System.ComponentModel.DataAnnotations;
using HamgamTransport.Server.Data;

namespace HamgamTransport.Server.Data.Models.Finance;

// دوره ماهانه شمسی — فقط قفل ثبت (بدون صفر کردن موقت)
public class FiscalPeriod : BaseEntity
{
    [Key]
    public int FiscalPeriodID { get; set; }

    public int SolarYear { get; set; }

    // ماه شمسی ۱ تا ۱۲
    public int Month { get; set; }

    public FiscalYearStatus Status { get; set; } = FiscalYearStatus.Open;

    public DateTime? ClosedAt { get; set; }

    public int? ClosedByUserId { get; set; }
}
