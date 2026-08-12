using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data.Models.Finance;

namespace HamgamTransport.Server.Data.Models.People
{
    public class Shareholder : BaseEntity
    {
        [Key]
        public int ShareholderID { get; set; }
        public PersonTitle Title { get; set; } = PersonTitle.Mr;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { set; get; } = string.Empty;
        [Column(TypeName = "decimal(18,4)")]
        public decimal InitialBalance { get; set; } = 0;
        public string? Description { get; set; } = string.Empty;
        // سهم سود جداگانه
        [Column(TypeName = "decimal(18,2)")]
        public decimal ProfitShare { get; set; } = 0;

        // سهم ضرر جداگانه
        [Column(TypeName = "decimal(18,2)")]
        public decimal LossShare { get; set; } = 0;

        // حساب تفصیلی سرمایه زیر معین ۳۱۱
        public int? AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public virtual Account? Account { get; set; }
    }
}
