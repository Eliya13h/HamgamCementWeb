using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data.Models.Finance;

namespace HamgamCementWeb.Server.Data.Models.People;

/// <summary>
/// پرداخت حقوق ماهانه کارمند — لایه عملیاتی؛ سند دفتر با JournalEntryId.
/// </summary>
public class SalaryPayment : BaseEntity
{
    [Key]
    public int SalaryPaymentID { get; set; }

    public int EmployeeId { get; set; }

    // دوره حقوق — معمولاً سال/ماه شمسی که کاربر انتخاب می‌کند
    public int Year { get; set; }
    public int Month { get; set; }

    public DateTime PaymentDate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal BaseSalary { get; set; }

    // مبالغ دستی (قابل ویرایش هنگام ثبت)
    [Column(TypeName = "decimal(18,4)")]
    public decimal OvertimeAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LateDeduction { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AbsenceDeduction { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal BenefitAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal OtherDeduction { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal NetAmount { get; set; }

    // خلاصه حضور برای گزارش
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalOvertimeMinutes { get; set; }

    public int? CashBoxId { get; set; }

    // سند دفترروزنامه متناظر
    public int? JournalEntryId { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public virtual Employee? Employee { get; set; }

    [ForeignKey(nameof(CashBoxId))]
    public virtual CashBox? CashBox { get; set; }

    [ForeignKey(nameof(JournalEntryId))]
    public virtual JournalEntry? JournalEntry { get; set; }
}
