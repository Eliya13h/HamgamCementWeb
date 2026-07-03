using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;



namespace HamgamCementWeb.Server.Data.Models.Product

{

    public class Meaurment : BaseEntity

    {

        [Key]

        public int MeaurmentID { get; set; }



        [MaxLength(100)]

        public string Name { get; set; } = string.Empty;



        [MaxLength(20)]

        public string? Symbol { get; set; }



        // true = واحد پایه (مثل کیلو، متر) — چند واحد پایه مستقل داریم

        public bool IsBaseUnit { get; set; }



        // برای واحد مشتق: اشاره به واحد پایه خانواده (مثل تن → کیلو)

        public int? BaseMeaurmentId { get; set; }



        // هر واحد مشتق معادل چند واحد پایه خانواده است (مثلاً ۱ تن = ۱۰۰۰ کیلو)

        [Column(TypeName = "decimal(18,6)")]

        public decimal FactorToBase { get; set; } = 1;



        [ForeignKey(nameof(BaseMeaurmentId))]

        public virtual Meaurment? BaseMeaurment { get; set; }



        public virtual ICollection<Meaurment> DerivedUnits { get; set; } = [];



        public virtual ICollection<ProductMeaurment> ProductMeaurments { get; set; } = [];

    }

}


