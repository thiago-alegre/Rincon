using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models.ViewModels;
using Rincon.Utilities;
using System.Globalization;
using System.Text;

namespace Rincon.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class StatisticsController : Controller
    {
        private readonly IWorkContainer _workContainer;

        public StatisticsController(IWorkContainer workContainer)
        {
            _workContainer = workContainer;
        }

        public IActionResult Index(DateTime? dateFrom, DateTime? dateTo, string? groupBy, string? metric)
        {
            var vm = BuildStatistics(dateFrom, dateTo, groupBy, metric);
            return View(vm);
        }

        [HttpGet]
        public IActionResult ExportCsv(DateTime? dateFrom, DateTime? dateTo, string? groupBy, string? metric)
        {
            var today = DateTime.Today;
            var from = (dateFrom ?? today.AddMonths(-1)).Date;
            var to = (dateTo ?? today).Date;

            if (to < from)
            {
                (from, to) = (to, from);
            }

            var toExclusive = to.AddDays(1);

            var saleDetails = _workContainer.SaleDetail
                .GetAll(
                    d => d.Sale.Date >= from && d.Sale.Date < toExclusive,
                    includeProperties: "Sale,Sale.User,Article,Article.Category")
                .OrderBy(d => d.Sale.Date)
                .ThenBy(d => d.SaleId)
                .ToList();

            var csv = new StringBuilder();
            csv.AppendLine("Venta Id;Fecha;Usuario;Email usuario;Medio de pago;Total venta;Monto recibido;Vuelto;Articulo Id;Producto;Codigo;Categoria;Unidad;Cantidad;Precio unitario;Costo unitario;Subtotal;Ganancia estimada");

            foreach (var detail in saleDetails)
            {
                var unitCost = detail.Article?.Cost ?? 0m;
                var estimatedProfit = (detail.UnitPrice - unitCost) * detail.Quantity;
                var user = detail.Sale.User;

                csv.AppendLine(string.Join(";",
                    detail.SaleId.ToString(),
                    EscapeCsv(detail.Sale.Date.ToString("dd/MM/yyyy HH:mm")),
                    EscapeCsv(user?.FullName ?? "Sin usuario"),
                    EscapeCsv(user?.Email ?? string.Empty),
                    EscapeCsv(GetPaymentMethodDisplay(detail.Sale.PaymentMethod.ToString())),
                    FormatDecimal(detail.Sale.Total),
                    FormatNullableDecimal(detail.Sale.AmountReceived),
                    FormatNullableDecimal(detail.Sale.Change),
                    detail.ArticleId.ToString(),
                    EscapeCsv(detail.ArticleName),
                    EscapeCsv(detail.ArticleCode),
                    EscapeCsv(detail.Article?.Category?.Name ?? "Sin categoria"),
                    EscapeCsv(detail.UnitOfMeasure),
                    FormatDecimal(detail.Quantity),
                    FormatDecimal(detail.UnitPrice),
                    FormatDecimal(unitCost),
                    FormatDecimal(detail.Subtotal),
                    FormatDecimal(estimatedProfit)));
            }

            var bytes = Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(csv.ToString()))
                .ToArray();

            return File(bytes, "text/csv", $"ventas-rincon-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
        }

        private StatisticsVM BuildStatistics(DateTime? dateFrom, DateTime? dateTo, string? groupBy, string? metric)
        {
            var today = DateTime.Today;
            var from = (dateFrom ?? today.AddMonths(-1)).Date;
            var to = (dateTo ?? today).Date;

            if (to < from)
            {
                (from, to) = (to, from);
            }

            var group = NormalizeGroupBy(groupBy);
            var selectedMetric = NormalizeMetric(metric);
            var toExclusive = to.AddDays(1);

            var sales = _workContainer.Sale
                .GetAll(s => s.Date >= from && s.Date < toExclusive)
                .ToList();

            var saleDetails = _workContainer.SaleDetail
                .GetAll(
                    d => d.Sale.Date >= from && d.Sale.Date < toExclusive,
                    includeProperties: "Sale,Article,Article.Category")
                .ToList();

            var productStats = saleDetails
                .GroupBy(d => new
                {
                    d.ArticleId,
                    Product = d.ArticleName,
                    Category = d.Article?.Category?.Name ?? "Sin categoria"
                })
                .Select(g => new StatisticsProductPointVM
                {
                    Product = g.Key.Product,
                    Category = g.Key.Category,
                    Quantity = g.Sum(x => x.Quantity),
                    Amount = g.Sum(x => x.Subtotal),
                    EstimatedProfit = g.Sum(x => (x.UnitPrice - (x.Article?.Cost ?? 0m)) * x.Quantity)
                })
                .ToList();

            var topByQuantity = productStats
                .OrderByDescending(p => p.Quantity)
                .FirstOrDefault();

            var topByRevenue = productStats
                .OrderByDescending(p => p.Amount)
                .FirstOrDefault();

            var periodSeries = saleDetails
                .GroupBy(d => GetPeriodKey(d.Sale.Date, group))
                .OrderBy(g => g.Key.Sort)
                .Select(g => new StatisticsPeriodPointVM
                {
                    Period = g.Key.Label,
                    Amount = g.Sum(x => x.Subtotal),
                    Quantity = g.Sum(x => x.Quantity)
                })
                .ToList();

            var categoryDistribution = saleDetails
                .GroupBy(d => d.Article?.Category?.Name ?? "Sin categoria")
                .Select(g => new StatisticsCategoryPointVM
                {
                    Category = g.Key,
                    Amount = g.Sum(x => x.Subtotal),
                    Quantity = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(c => selectedMetric == "amount" ? c.Amount : c.Quantity)
                .ToList();

            var lowStockItems = _workContainer.Article
                .GetAll(a => a.isActive)
                .Where(a => a.Stock <= a.StockMin + 5)
                .OrderBy(a => a.Stock <= a.StockMin ? 0 : 1)
                .ThenBy(a => a.Stock - a.StockMin)
                .Select(a => new StatisticsLowStockItemVM
                {
                    Product = a.Name,
                    Stock = a.Stock,
                    StockMin = a.StockMin,
                    UnitOfMeasure = a.UnitOfMeasure,
                    Status = a.Stock <= a.StockMin ? "danger" : "warning",
                    StatusText = a.Stock <= a.StockMin ? "Bajo minimo" : "Cerca del minimo"
                })
                .Take(10)
                .ToList();

            return new StatisticsVM
            {
                DateFrom = from,
                DateTo = to,
                GroupBy = group,
                Metric = selectedMetric,
                Summary = new StatisticsSummaryVM
                {
                    TotalRevenue = sales.Sum(s => s.Total),
                    SalesCount = sales.Count,
                    AverageTicket = sales.Count > 0 ? sales.Average(s => s.Total) : 0m,
                    EstimatedProfit = productStats.Sum(p => p.EstimatedProfit),
                    TotalProductsSold = productStats.Sum(p => p.Quantity),
                    BestSellingProduct = topByQuantity?.Product ?? "Sin datos",
                    TopRevenueProduct = topByRevenue?.Product ?? "Sin datos"
                },
                PeriodSeries = periodSeries,
                CategoryDistribution = categoryDistribution,
                TopProducts = productStats
                    .OrderByDescending(p => selectedMetric == "amount" ? p.Amount : p.Quantity)
                    .Take(10)
                    .ToList(),
                LowStockItems = lowStockItems
            };
        }

        private static string NormalizeGroupBy(string? groupBy)
        {
            return groupBy switch
            {
                "monthly" => "monthly",
                "yearly" => "yearly",
                _ => "daily"
            };
        }

        private static string NormalizeMetric(string? metric)
        {
            return metric == "quantity" ? "quantity" : "amount";
        }

        private static (string Label, DateTime Sort) GetPeriodKey(DateTime date, string groupBy)
        {
            return groupBy switch
            {
                "monthly" => (date.ToString("MM/yyyy"), new DateTime(date.Year, date.Month, 1)),
                "yearly" => (date.ToString("yyyy"), new DateTime(date.Year, 1, 1)),
                _ => (date.ToString("dd/MM/yyyy"), date.Date)
            };
        }

        private static string EscapeCsv(string? value)
        {
            value ??= string.Empty;
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private static string FormatDecimal(decimal value)
        {
            return value.ToString("0.###", CultureInfo.GetCultureInfo("es-AR"));
        }

        private static string FormatNullableDecimal(decimal? value)
        {
            return value.HasValue ? FormatDecimal(value.Value) : string.Empty;
        }

        private static string GetPaymentMethodDisplay(string paymentMethod)
        {
            return paymentMethod == "CuentaPersonal" ? "Cuenta personal" : paymentMethod;
        }
    }
}
