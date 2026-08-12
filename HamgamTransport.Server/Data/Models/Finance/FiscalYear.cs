using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.People;

namespace HamgamTransport.Server.Data.Models.Finance;

// سال مالی شمسی — بستن دستی فقط توسط نقش مدیر سیستم
public class FiscalYear : BaseEntity
{
    [Key]
    public int FiscalYearID { get; set; }

    // سال شمسی (مثلاً ۱۴۰۴)
    public int SolarYear { get; set; }

    // ابتدا و انتهای سال به میلادی برای فیلتر اسناد
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public FiscalYearStatus Status { get; set; } = FiscalYearStatus.Open;

    public DateTime? ClosedAt { get; set; }

    public int? ClosedByUserId { get; set; }

    public int? ClosingJournalEntryId { get; set; }

    // سند تخصیص سود/زیان به تفصیلی سهامداران پس از اختتام
    public int? EquityAllocationJournalEntryId { get; set; }

    // سود/زیان خالص سال به ارز پایه — برای خلاصه لیست
    [Column(TypeName = "decimal(18,4)")]
    public decimal NetIncomeInBaseCurrency { get; set; }

    [ForeignKey(nameof(ClosedByUserId))]
    public virtual User? ClosedByUser { get; set; }

    [ForeignKey(nameof(ClosingJournalEntryId))]
    public virtual JournalEntry? ClosingJournalEntry { get; set; }

    [ForeignKey(nameof(EquityAllocationJournalEntryId))]
    public virtual JournalEntry? EquityAllocationJournalEntry { get; set; }
}
