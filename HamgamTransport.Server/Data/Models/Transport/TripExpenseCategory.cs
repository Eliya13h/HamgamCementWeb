using System.ComponentModel.DataAnnotations;

namespace HamgamTransport.Server.Data.Models.Transport;

public class TripExpenseCategory : BaseEntity
{
    [Key]
    public int TripExpenseCategoryId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
