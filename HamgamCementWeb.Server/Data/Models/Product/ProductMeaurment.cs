using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.Product
{
    // واحدهای مجاز برای هر محصول — تبدیل خودکار از طریق FactorToBase واحدها
    public class ProductMeaurment : BaseEntity
    {
        [Key]
        public int ProductMeaurmentID { get; set; }

        public int ProductId { get; set; }
        public int MeaurmentId { get; set; }

        public bool IsDefault { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey(nameof(MeaurmentId))]
        public virtual Meaurment Meaurment { get; set; } = null!;
    }
}
