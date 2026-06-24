using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rincon.Models
{
    public class SaleDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SaleId { get; set; }

        [ForeignKey("SaleId")]
        public Sale Sale { get; set; }

        public int? ArticleId { get; set; }

        [ForeignKey("ArticleId")]
        public Article? Article { get; set; }

        [Required]
        public string ArticleName { get; set; }

        public string? ArticleCode { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedProfit { get; set; }

        [Required]
        public string UnitOfMeasure { get; set; }

        public ICollection<SaleDetailBatch> SaleDetailBatches { get; set; } = new List<SaleDetailBatch>();
        public ICollection<SaleReturnDetail> SaleReturnDetails { get; set; } = new List<SaleReturnDetail>();
        public ICollection<SaleExchange> SaleExchanges { get; set; } = new List<SaleExchange>();
    }
}
