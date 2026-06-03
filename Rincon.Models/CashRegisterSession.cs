using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rincon.Models
{
    public class CashRegisterSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime OpenedAt { get; set; } = DateTime.Now;

        public DateTime? ClosedAt { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CountedCashAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ExpectedCashAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Difference { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public ICollection<PersonalAccountPayment> PersonalAccountPayments { get; set; } = new List<PersonalAccountPayment>();

        [NotMapped]
        public bool IsOpen => ClosedAt == null;
    }
}
