using System.ComponentModel.DataAnnotations;

namespace HamgamCementWeb.Server.Data.Models.Product
{
    // دسته‌بندی محصولات
    public class Category : BaseEntity
    {
        [Key]
        public int CategoryID { get; set; }

        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int? ParentCategoryId { get; set; }

        public virtual Category? ParentCategory { get; set; }
        public virtual ICollection<Category> Children { get; set; } = [];
        public virtual ICollection<ProductCategory> ProductCategories { get; set; } = [];
    }
}
