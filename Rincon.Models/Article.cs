using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rincon.Models
{
    public class Article
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ingrese un nombre para el artículo")]
        [Display(Name = "Nombre del artículo")]
        public string Name { get; set; }

        [Display(Name = "Descripción")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Ingrese un código para el artículo")]
        [Display(Name = "Código")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Ingrese un precio")]
        [Display(Name = "Precio de venta")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Ingrese un costo")]
        [Display(Name = "Costo")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El costo debe ser mayor a 0")]
        public decimal Cost { get; set; }

        [Required(ErrorMessage = "Ingrese el stock")]
        [Display(Name = "Stock")]
        [Range(0, double.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public decimal Stock { get; set; }

        [Display(Name = "Stock mínimo")]
        [Range(0, double.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo")]
        public decimal StockMin { get; set; }

        [Display(Name = "¿Se vende por peso?")]
        public bool IsSoldByWeight { get; set; } = false;

        [Display(Name = "Unidad de medida")]
        public string UnitOfMeasure { get; set; } = "Unidad";

        [Display(Name = "Fecha de vencimiento")]
        public DateTime? ExpirationDate { get; set; }

        [Display(Name = "Imagen")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Fecha de creación")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Display(Name = "Estado")]
        public bool isActive { get; set; } = true;

        [Required(ErrorMessage = "Seleccione una categoría")]
        [Display(Name = "Categoría")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
    }
}