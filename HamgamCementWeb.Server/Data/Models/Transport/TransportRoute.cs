using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.Transport
{
    // مسیر حمل و نقل (از هر نقطه دنیا به افغانستان)
    public class TransportRoute : BaseEntity
    {
        [Key]
        public int TransportRouteID { get; set; }

        // کد خودکار مسیر (مثلاً HMR0001)
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        // نام مسیر (مثلاً بندرعباس - نیمروز)
        [MaxLength(300)]
        public string Name { get; set; } = string.Empty;

        // مبدأ مسیر
        [MaxLength(200)]
        public string Origin { get; set; } = string.Empty;

        // کشور مبدأ
        [MaxLength(100)]
        public string? OriginCountry { get; set; }

        // مقصد مسیر
        [MaxLength(200)]
        public string Destination { get; set; } = string.Empty;

        // مسافت مسیر به کیلومتر
        [Column(TypeName = "decimal(18,4)")]
        public decimal? DistanceKm { get; set; }

        // مدت تقریبی سفر به روز
        public int? EstimatedDays { get; set; }

        public string? Description { get; set; }

        public virtual ICollection<TransportTrip> Trips { get; set; } = [];
    }
}
