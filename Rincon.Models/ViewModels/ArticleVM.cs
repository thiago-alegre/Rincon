using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Rincon.Models.ViewModels
{
    public class ArticleVM
    {
        public Article Article { get; set; } = new Article();

        public IEnumerable<SelectListItem> CategoryList { get; set; } = new List<SelectListItem>();

        [Required(ErrorMessage = "Ingrese un precio")]
        [Display(Name = "Precio de venta")]
        public string? PriceText { get; set; }

        [Required(ErrorMessage = "Ingrese un costo")]
        [Display(Name = "Costo")]
        public string? CostText { get; set; }

        [Required(ErrorMessage = "Ingrese el stock")]
        [Display(Name = "Stock")]
        public string? StockText { get; set; }

        [Display(Name = "Stock mínimo")]
        public string? StockMinText { get; set; }
    }
}
