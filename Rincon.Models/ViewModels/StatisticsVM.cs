namespace Rincon.Models.ViewModels
{
    public class StatisticsVM
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string GroupBy { get; set; } = "daily";
        public string Metric { get; set; } = "amount";

        public StatisticsSummaryVM Summary { get; set; } = new();
        public List<StatisticsPeriodPointVM> PeriodSeries { get; set; } = new();
        public List<StatisticsCategoryPointVM> CategoryDistribution { get; set; } = new();
        public List<StatisticsProductPointVM> TopProducts { get; set; } = new();
        public List<StatisticsUserPointVM> UserSales { get; set; } = new();
        public List<StatisticsLowStockItemVM> LowStockItems { get; set; } = new();
    }

    public class StatisticsSummaryVM
    {
        public decimal TotalRevenue { get; set; }
        public int SalesCount { get; set; }
        public decimal AverageTicket { get; set; }
        public decimal EstimatedProfit { get; set; }
        public decimal TotalProductsSold { get; set; }
        public string BestSellingProduct { get; set; } = "Sin datos";
        public string TopRevenueProduct { get; set; } = "Sin datos";
    }

    public class StatisticsPeriodPointVM
    {
        public string Period { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Quantity { get; set; }
    }

    public class StatisticsCategoryPointVM
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Quantity { get; set; }
    }

    public class StatisticsProductPointVM
    {
        public string Product { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
        public decimal EstimatedProfit { get; set; }
    }

    public class StatisticsUserPointVM
    {
        public string User { get; set; } = string.Empty;
        public int SalesCount { get; set; }
        public decimal Amount { get; set; }
    }

    public class StatisticsLowStockItemVM
    {
        public string Product { get; set; } = string.Empty;
        public decimal Stock { get; set; }
        public decimal StockMin { get; set; }
        public string UnitOfMeasure { get; set; } = "Unidad";
        public string Status { get; set; } = "warning";
        public string StatusText { get; set; } = "Cerca del mínimo";
    }
}
