using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        [Display(Name = "Precio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Ingrese el stock")]
        [Display(Name = "Stock")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public int Stock { get; set; }

        [Display(Name = "Stock mínimo")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo")]
        public int StockMin { get; set; }

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