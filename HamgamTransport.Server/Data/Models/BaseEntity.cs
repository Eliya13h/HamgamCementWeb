using HamgamTransport.Server.Data.Models.People;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamTransport.Server.Data.Models
{
    public class BaseEntity
    {
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public bool? IsActive { get; set; } = true;
        public bool? IsDeleted { get; set; } = false;
        public bool? IsUpdated { get; set; } = false;

        // کلیدهای خارجی
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation Properties جدا
        [ForeignKey(nameof(CreatedBy))]
        public virtual User? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        public virtual User? UpdatedByUser { get; set; }

        [ForeignKey(nameof(DeletedBy))]
        public virtual User? DeletedByUser { get; set; }
    }



}
