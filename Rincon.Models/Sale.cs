using Rincon.Utilities.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rincon.Models
{
    public class Sale
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? AmountReceived { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Change { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public int? CashRegisterSessionId { get; set; }

        [ForeignKey("CashRegisterSessionId")]
        public CashRegisterSession? CashRegisterSession { get; set; }

        public int? PersonalAccountId { get; set; }

        [ForeignKey("PersonalAccountId")]
        public PersonalAccount? PersonalAccount { get; set; }

        public bool IsPersonalAccountSettled { get; set; } = false;

        public DateTime? PersonalAccountSettledAt { get; set; }

        public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
    }
}
