using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rincon.Models
{
    public class ArticleBatch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ArticleId { get; set; }

        [ForeignKey("ArticleId")]
        public Article? Article { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal InitialQuantity { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.Today;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Cost { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
