namespace Rincon.Models.ViewModels
{
    public class CartItemVM
    {
        public string LineId { get; set; } = string.Empty;
        public Article Article { get; set; } = new Article();
        public decimal Quantity { get; set; }
        public bool IsManual { get; set; }
        public string? ManualName { get; set; }
        public decimal ManualUnitPrice { get; set; }
        public string UnitOfMeasure { get; set; } = "Unidad";
        public decimal RemainingStock => IsManual ? 0 : Article.Stock - Quantity;
        public bool ExceedsStock => !IsManual && Quantity > Article.Stock;
        public bool ReachesMinimumStock => !IsManual && RemainingStock <= Article.StockMin;
        public decimal LineTotal
        {
            get
            {
                return IsManual ? ManualUnitPrice * Quantity : Article.Price * Quantity;
            }
        }
    }
}
