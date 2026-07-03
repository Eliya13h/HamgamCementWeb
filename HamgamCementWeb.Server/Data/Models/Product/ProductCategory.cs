using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.Product
{
    // ارتباط چند-به-چند محصول و دسته‌بندی
    public class ProductCategory : BaseEntity
    {
        [Key]
        public int ProductCategoryID { get; set; }

        public int ProductId { get; set; }
        public int CategoryId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey(nameof(CategoryId))]
        public virtual Category Category { get; set; } = null!;
    }
}
