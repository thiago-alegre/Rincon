namespace Rincon.Models.ViewModels
{
    public class ShoppingCartItemVM
    {
        public string LineId { get; set; } = Guid.NewGuid().ToString("N");
        public int? ArticleId { get; set; }
        public decimal Quantity { get; set; }
        public bool IsManual { get; set; }
        public string? ManualName { get; set; }
        public decimal ManualUnitPrice { get; set; }
        public string UnitOfMeasure { get; set; } = "Unidad";
    }
}
