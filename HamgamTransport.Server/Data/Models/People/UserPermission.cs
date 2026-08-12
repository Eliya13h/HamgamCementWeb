using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamTransport.Server.Data.Models.People;

public class UserPermission
{
    [Key]
    public int UserPermissionID { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    // کلید اجازه مثل people.customers.view
    [MaxLength(120)]
    public string PermissionKey { get; set; } = string.Empty;
}
