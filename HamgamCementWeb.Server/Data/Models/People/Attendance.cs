using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.People;

/// <summary>
/// حضور روزانه کارمند — فقط برای تاریخچه و محاسبه حقوق.
/// </summary>
public class Attendance : BaseEntity
{
    [Key]
    public int AttendanceID { get; set; }

    public int EmployeeId { get; set; }

    // فقط تاریخ روز (بدون ساعت)
    public DateTime Date { get; set; }

    // تیک حضور
    public bool IsPresent { get; set; }

    // دیرکرد به دقیقه (ثبت دستی)
    public int LateMinutes { get; set; }

    // اضافه‌کاری به دقیقه (ثبت دستی)
    public int OvertimeMinutes { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public virtual Employee? Employee { get; set; }
}
