using System.ComponentModel.DataAnnotations;

namespace HamgamCementWeb.Server.Data.Models.People
{
    public class Department : BaseEntity
    {
        [Key]
        public int DepartmentID { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSelected { get; set; } = false;

        public ICollection<Employee> Employees { get; set; } = [];
    }
}
