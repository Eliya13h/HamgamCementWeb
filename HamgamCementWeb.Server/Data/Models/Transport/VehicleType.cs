using System.ComponentModel.DataAnnotations;

namespace HamgamCementWeb.Server.Data.Models.Transport
{
    // نوع وسیله نقلیه (کشنده، بونکر، اسب و ...) — توسط کاربر قابل ثبت است
    public class VehicleType : BaseEntity
    {
        [Key]
        public int VehicleTypeID { get; set; }

        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public virtual ICollection<Vehicle> Vehicles { get; set; } = [];
    }
}
