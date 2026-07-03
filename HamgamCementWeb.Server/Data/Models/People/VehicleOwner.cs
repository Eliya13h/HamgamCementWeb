using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.People
{
    public class VehicleOwner : BaseEntity
    {
        [Key]
        public int VehicleOwnerID { set; get; }

        public PersonTitle Title { get; set; } = PersonTitle.Mr;
        public string Name { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // سهم پیش‌فرض موتردار
        [Column(TypeName = "decimal(18,2)")]
        public decimal DefaultShare { get; set; }
    }
}
