using Microsoft.AspNetCore.Mvc.Rendering;

namespace Rincon.Models.ViewModels
{
    public class CartVM
    {
        public IEnumerable<CartItemVM> Items { get; set; } = new List<CartItemVM>();
        public IEnumerable<Article> SearchResults { get; set; } = new List<Article>();
        public IEnumerable<SelectListItem> PersonalAccountList { get; set; } = new List<SelectListItem>();
        public string? SearchString { get; set; }
        public bool HasStockProblems => Items.Any(i => i.ExceedsStock);
        public bool HasMinimumStockWarnings => Items.Any(i => !i.ExceedsStock && i.ReachesMinimumStock);
        public decimal Total => Items.Sum(i => i.LineTotal);
    }
}
