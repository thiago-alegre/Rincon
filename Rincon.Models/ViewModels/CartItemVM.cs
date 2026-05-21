namespace Rincon.Models.ViewModels
{
    public class CartItemVM
    {
        public Article Article { get; set; } = new Article();
        public decimal Quantity { get; set; }
        public decimal RemainingStock => Article.Stock - Quantity;
        public bool ExceedsStock => Quantity > Article.Stock;
        public bool ReachesMinimumStock => RemainingStock <= Article.StockMin;
        public decimal LineTotal
        {
            get
            {
                return Article.Price * Quantity;
            }
        }
    }
}
