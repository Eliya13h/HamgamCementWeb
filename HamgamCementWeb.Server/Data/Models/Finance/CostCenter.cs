using System.ComponentModel.DataAnnotations;

namespace HamgamCementWeb.Server.Data.Models.Finance;

// مرکز هزینه — بعد تحلیلی اختیاری روی خطوط سند
public class CostCenter : BaseEntity
{
    [Key]
    public int CostCenterID { get; set; }

    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}
