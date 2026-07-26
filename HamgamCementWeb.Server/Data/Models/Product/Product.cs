using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

using HamgamCementWeb.Server.Data;



namespace HamgamCementWeb.Server.Data.Models.Product

{

    public class Product : BaseEntity

    {

        [Key]

        public int ProductID { get; set; }



        [MaxLength(50)]

        public string Code { get; set; } = string.Empty;



        [MaxLength(300)]

        public string Name { get; set; } = string.Empty;



        [MaxLength(2000)]

        public string? Description { get; set; }



        // واحد پایه این محصول — مثلاً کیلو یا متر

        public int BaseMeaurmentId { get; set; }



        public int? DefaultMeaurmentId { get; set; }



        // منسوخ برای ورود داده: قیمت خرید دیگر در فرم محصول ویرایش نمی‌شود؛
        // پیشنهاد لحظه‌ای از میانگین لات/آخرین خرید محاسبه می‌شود. ستون برای سازگاری اسکیما نگه داشته شد.
        [Column(TypeName = "decimal(18,4)")]

        public decimal DefaultPurchasePrice { get; set; }



        [Column(TypeName = "decimal(18,4)")]

        public decimal DefaultSalePrice { get; set; }



        // حداقل موجودی به واحد پایه — برای اخطار کاهش موجودی

        [Column(TypeName = "decimal(18,6)")]

        public decimal MinStockQuantity { get; set; }



        [ForeignKey(nameof(BaseMeaurmentId))]

        public virtual Meaurment BaseMeaurment { get; set; } = null!;



        [ForeignKey(nameof(DefaultMeaurmentId))]

        public virtual Meaurment? DefaultMeaurment { get; set; }



        public virtual ICollection<ProductMeaurment> ProductMeaurments { get; set; } = [];

        public virtual ICollection<ProductCategory> ProductCategories { get; set; } = [];

    }

}


