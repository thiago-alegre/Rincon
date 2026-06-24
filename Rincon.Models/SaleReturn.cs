using Rincon.Utilities.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rincon.Models
{
    public class SaleReturn
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SaleId { get; set; }

        [ForeignKey("SaleId")]
        public Sale? Sale { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public int? CashRegisterSessionId { get; set; }

        [ForeignKey("CashRegisterSessionId")]
        public CashRegisterSession? CashRegisterSession { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public bool IsFullVoid { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        public ICollection<SaleReturnDetail> SaleReturnDetails { get; set; } = new List<SaleReturnDetail>();
    }
}
