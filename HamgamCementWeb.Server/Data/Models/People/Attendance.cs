using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.People;

/// <summary>
/// خلاصه حضور ماهانه کارمند (سال/ماه شمسی) — برای حقوق و گزارش.
/// </summary>
public class Attendance : BaseEntity
{
    [Key]
    public int AttendanceID { get; set; }

    public int EmployeeId { get; set; }

    // سال شمسی دوره حضور
    public int Year { get; set; }

    // ماه شمسی دوره حضور (۱ تا ۱۲)
    public int Month { get; set; }

    // تعداد روز حاضر
    public int PresentDays { get; set; }

    // تعداد روز غیرحاضر
    public int AbsentDays { get; set; }

    // رخصت با حقوق
    public int LeavePaidDays { get; set; }

    // رخصت بدون حقوق
    public int LeaveUnpaidDays { get; set; }

    // تعطیل با حقوق
    public int HolidayPaidDays { get; set; }

    // تعطیل بدون حقوق
    public int HolidayUnpaidDays { get; set; }

    // تأخیر به ساعت
    [Column(TypeName = "decimal(10,2)")]
    public decimal LateHours { get; set; }

    // تعجیل در خروج به ساعت
    [Column(TypeName = "decimal(10,2)")]
    public decimal EarlyLeaveHours { get; set; }

    // اضافه‌کار به ساعت
    [Column(TypeName = "decimal(10,2)")]
    public decimal OvertimeHours { get; set; }

    // ضریب اضافه‌کار برای محاسبه هزینه
    [Column(TypeName = "decimal(8,4)")]
    public decimal OvertimeCoefficient { get; set; } = 1.5m;

    [MaxLength(2000)]
    public string? Note { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public virtual Employee? Employee { get; set; }
}
