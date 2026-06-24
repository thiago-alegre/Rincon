using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rincon.Models
{
    public class SaleReturnDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SaleReturnId { get; set; }

        [ForeignKey("SaleReturnId")]
        public SaleReturn? SaleReturn { get; set; }

        [Required]
        public int SaleDetailId { get; set; }

        [ForeignKey("SaleDetailId")]
        public SaleDetail? SaleDetail { get; set; }

        public int? ArticleId { get; set; }

        [ForeignKey("ArticleId")]
        public Article? Article { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Required]
        public string UnitOfMeasure { get; set; } = string.Empty;

        public ICollection<SaleReturnDetailBatch> SaleReturnDetailBatches { get; set; } = new List<SaleReturnDetailBatch>();
    }
}
