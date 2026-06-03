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
        public decimal Subtotal { get; set; }

        [Required]
        public string UnitOfMeasure { get; set; }
    }
}
