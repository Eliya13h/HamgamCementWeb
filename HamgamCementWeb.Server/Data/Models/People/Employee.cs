using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.People
{
    public class Employee : BaseEntity
    {
        [Key]
        public int EmployeeID { get; set; }
        public PersonTitle Title { get; set; } = PersonTitle.Mr;
        public string Name { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        
        public string AvatarUrl { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,4)")]
        public decimal Sallary { get; set; }
        public int DepartmentId { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public virtual Department? Department { get; set; }

        public virtual User? User { get; set; }
    }
}
