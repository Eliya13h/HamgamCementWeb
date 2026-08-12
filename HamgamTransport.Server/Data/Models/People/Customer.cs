using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamTransport.Server.Data.Models.People
{
    public class Customer : BaseEntity
    {
        [Key]
        public int CustomerID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,4)")]
        public decimal InitialBalance { get; set; } = 0;
        public PersonType CustomerType { get; set; } = PersonType.NaturalPerson;
    }
}
