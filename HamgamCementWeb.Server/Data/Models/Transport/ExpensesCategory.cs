using System.ComponentModel.DataAnnotations;

namespace HamgamCementWeb.Server.Data.Models.Transport
{
    // دسته‌بندی مصارف حمل و نقل (سوخت، روغن، لاستیک، تعمیرات و ...) — توسط کاربر قابل ثبت است
    public class ExpensesCategory : BaseEntity
    {
        [Key]
        public int ExpensesCategoryID { get; set; }

        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public virtual ICollection<TransportExpense> Expenses { get; set; } = [];
    }
}
