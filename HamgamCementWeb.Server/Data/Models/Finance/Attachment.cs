using System.ComponentModel.DataAnnotations;

namespace HamgamCementWeb.Server.Data.Models.Finance;

// پیوست عمومی — فعلاً برای JournalEntry (EntityType = JournalEntry)
public class Attachment : BaseEntity
{
    [Key]
    public int AttachmentID { get; set; }

    // نام نوع موجودیت (مثلاً JournalEntry)
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    // شناسه موجودیت متصل
    public int EntityId { get; set; }

    [MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(260)]
    public string StoredFileName { get; set; } = string.Empty;

    // مسیر نسبی زیر wwwroot
    [MaxLength(500)]
    public string RelativePath { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? ContentType { get; set; }

    public long SizeBytes { get; set; }
}
