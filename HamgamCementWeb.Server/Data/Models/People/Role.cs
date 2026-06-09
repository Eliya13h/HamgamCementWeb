using System.ComponentModel.DataAnnotations;

namespace HamgamCementWeb.Server.Data.Models.People
{
    public class Role : BaseEntity
    {
        [Key]
        public int RoleID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description {  get; set; } = string.Empty;
        public ICollection<User> Users { get; set; } = [];
    }
}
