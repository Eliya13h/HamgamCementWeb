using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data.Models.Product;
using ProductEntity = HamgamTransport.Server.Data.Models.Product.Product;

namespace HamgamTransport.Server.Data.Models.Inventory
{
    // ردیف شمارش در انبارگردانی
    public class StocktakingLine : BaseEntity
    {
        [Key]
        public int StocktakingLineID { get; set; }

        public int StocktakingId { get; set; }
        public int ProductId { get; set; }

        // موجودی سیستم در لحظه شمارش (کیلوگرم)
        [Column(TypeName = "decimal(18,6)")]
        public decimal SystemQuantityInBase { get; set; }

        // مقدار شمارش‌شده توسط کاربر
        [Column(TypeName = "decimal(18,6)")]
        public decimal CountedQuantity { get; set; }

        public int CountedMeaurmentId { get; set; }

        // معادل شمارش به کیلوگرم
        [Column(TypeName = "decimal(18,6)")]
        public decimal CountedQuantityInBase { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal DifferenceInBase { get; set; }

        // بهای تعدیل در ارز پایه (|تفاوت| × بهای FIFO/میانگین) — پس از تأیید پر می‌شود
        [Column(TypeName = "decimal(18,4)")]
        public decimal AdjustmentCostInBase { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [ForeignKey(nameof(StocktakingId))]
        public virtual Stocktaking Stocktaking { get; set; } = null!;

        [ForeignKey(nameof(ProductId))]
        public virtual ProductEntity Product { get; set; } = null!;

        [ForeignKey(nameof(CountedMeaurmentId))]
        public virtual Meaurment CountedMeaurment { get; set; } = null!;
    }
}
