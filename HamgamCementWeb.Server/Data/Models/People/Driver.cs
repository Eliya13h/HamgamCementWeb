using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data.Models.Transport;

namespace HamgamCementWeb.Server.Data.Models.People
{
    public class Driver : BaseEntity
    {
        [Key]
        public int DriverID { set; get; }

        public PersonTitle Title { get; set; } = PersonTitle.Mr;
        public string Name { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // سهم پیش‌فرض راننده
        [Column(TypeName = "decimal(18,2)")]
        public decimal DefaultShare { get; set; }

        // وسیله نقلیه پیش‌فرض راننده — FK در Fluent API
        public int? DefaultVehicleId { get; set; }

        public virtual Vehicle? DefaultVehicle { get; set; }
    }
}
