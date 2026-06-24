using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rincon.Models
{
    public class SaleExchangeBatch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SaleExchangeId { get; set; }

        [ForeignKey("SaleExchangeId")]
        public SaleExchange? SaleExchange { get; set; }

        [Required]
        public int ArticleBatchId { get; set; }

        [ForeignKey("ArticleBatchId")]
        public ArticleBatch? ArticleBatch { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; }
    }
}
