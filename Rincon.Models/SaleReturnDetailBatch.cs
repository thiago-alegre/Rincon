using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rincon.Models
{
    public class SaleReturnDetailBatch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SaleReturnDetailId { get; set; }

        [ForeignKey("SaleReturnDetailId")]
        public SaleReturnDetail? SaleReturnDetail { get; set; }

        [Required]
        public int ArticleBatchId { get; set; }

        [ForeignKey("ArticleBatchId")]
        public ArticleBatch? ArticleBatch { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }
    }
}
