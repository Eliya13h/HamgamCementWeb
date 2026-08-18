using System.ComponentModel.DataAnnotations;

namespace HamgamTransport.Server.Data.Models.Transport;

public class TripExpenseCategory : BaseEntity
{
    [Key]
    public int TripExpenseCategoryId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // دسته تو در تو
    public int? ParentCategoryId { get; set; }
    public virtual TripExpenseCategory? Parent { get; set; }
    public virtual ICollection<TripExpenseCategory> Children { get; set; } = new List<TripExpenseCategory>();
}
