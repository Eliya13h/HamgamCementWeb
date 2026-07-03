using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.People
{
    public class User : BaseEntity
    {
        [Key]
        public int UserID { get; set; }
        public PersonTitle Title { get; set; } = PersonTitle.Mr;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int RoleId { get; set; }

        [ForeignKey(nameof(RoleId))]
        public virtual Role Role { get; set; } = null!;

        public string AvatarUrl { get; set; } = string.Empty;

        public int EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee Employee { get; set; } = null!;

        // دسترسی کامل به تمام بخش‌های سیستم — مستقل از نقش نمادین
        public bool HasFullAccess { get; set; } = true;

        public ICollection<UserPermission> Permissions { get; set; } = [];
    }
}
