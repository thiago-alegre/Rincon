using Rincon.Utilities.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rincon.Models
{
    public class PersonalAccountPayment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required]
        public int PersonalAccountId { get; set; }

        [ForeignKey("PersonalAccountId")]
        public PersonalAccount? PersonalAccount { get; set; }

        [Required]
        public int CashRegisterSessionId { get; set; }

        [ForeignKey("CashRegisterSessionId")]
        public CashRegisterSession? CashRegisterSession { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
    }
}
