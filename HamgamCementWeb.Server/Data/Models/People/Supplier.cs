using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.People
{
    public class Supplier : BaseEntity
    {
        [Key]
        public int SupplierID { get; set; }
        public PersonTitle Title { get; set; } = PersonTitle.Mr;
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,4)")]
        public decimal InitialBalance { get; set; } = 0;
        public PersonType SupplierType { get; set; } = PersonType.NaturalPerson;

    }





}
