using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rincon.Models
{
    public class SaleExchange
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SaleId { get; set; }

        [ForeignKey("SaleId")]
        public Sale? Sale { get; set; }

        [Required]
        public int SaleDetailId { get; set; }

        [ForeignKey("SaleDetailId")]
        public SaleDetail? SaleDetail { get; set; }

        public int? OriginalArticleId { get; set; }

        [ForeignKey("OriginalArticleId")]
        public Article? OriginalArticle { get; set; }

        [Required]
        public int ReplacementArticleId { get; set; }

        [ForeignKey("ReplacementArticleId")]
        public Article? ReplacementArticle { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ReplacementUnitCost { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedLoss { get; set; }

        [Required]
        public string UnitOfMeasure { get; set; } = string.Empty;

        public DateTime Date { get; set; } = DateTime.Now;

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public int? CashRegisterSessionId { get; set; }

        [ForeignKey("CashRegisterSessionId")]
        public CashRegisterSession? CashRegisterSession { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        public ICollection<SaleExchangeBatch> SaleExchangeBatches { get; set; } = new List<SaleExchangeBatch>();
    }
}
