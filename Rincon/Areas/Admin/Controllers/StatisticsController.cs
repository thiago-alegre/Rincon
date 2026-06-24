using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;
using Rincon.Models.ViewModels;
using Rincon.Utilities;
using Rincon.Utilities.Enums;
using System.Globalization;
using System.Security;
using System.Text;

namespace Rincon.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class StatisticsController : Controller
    {
        private readonly IWorkContainer _workContainer;
        private readonly UserManager<ApplicationUser> _userManager;

        public StatisticsController(IWorkContainer workContainer, UserManager<ApplicationUser> userManager)
        {
            _workContainer = workContainer;
            _userManager = userManager;
        }

        public IActionResult Index(DateTime? dateFrom, DateTime? dateTo, string? groupBy, string? metric, string? productMode, int? productLimit)
        {
            var vm = BuildStatistics(dateFrom, dateTo, groupBy, metric, productMode, productLimit);
            return View(vm);
        }

        [HttpGet]
        public IActionResult ExportExcel(DateTime? dateFrom, DateTime? dateTo, string? groupBy, string? metric)
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
                    d => d.Sale.Date >= from && d.Sale.Date < toExclusive && !d.Sale.IsVoided,
                    includeProperties: "Sale,Sale.User,Sale.PersonalAccount,Article,Article.Category")
                .OrderBy(d => d.Sale.Date)
                .ThenBy(d => d.SaleId)
                .ToList();

            var rows = new List<IEnumerable<object?>>();

            foreach (var detail in saleDetails)
            {
                var user = detail.Sale.User;

                rows.Add(new object?[]
                {
                    detail.SaleId,
                    detail.Sale.Date.ToString("dd/MM/yyyy HH:mm"),
                    user?.FullName ?? "Sin usuario",
                    user?.Email ?? string.Empty,
                    GetPaymentMethodDisplay(detail.Sale.PaymentMethod.ToString()),
                    detail.Sale.PersonalAccount?.FullName,
                    detail.Sale.PaymentMethod == PaymentMethod.CuentaPersonal
                        ? detail.Sale.IsPersonalAccountSettled ? "Saldada" : "Pendiente"
                        : string.Empty,
                    detail.Sale.Total,
                    detail.Sale.AmountReceived,
                    detail.Sale.Change,
                    detail.ArticleId.HasValue ? detail.ArticleId.Value.ToString() : string.Empty,
                    detail.ArticleName,
                    detail.ArticleCode,
                    detail.Article?.Category?.Name ?? "Sin categoría",
                    detail.UnitOfMeasure,
                    detail.Quantity,
                    detail.UnitPrice,
                    detail.UnitCost,
                    detail.Subtotal,
                    detail.EstimatedProfit
                });
            }

            return ExcelFile(
                $"ventas-rincon-{from:yyyyMMdd}-{to:yyyyMMdd}.xls",
                new ExcelSheet(
                    "Ventas",
                    new[]
                    {
                        "Venta Id", "Fecha", "Usuario", "Email usuario", "Medio de pago", "Cuenta personal",
                        "Estado cuenta personal", "Total venta", "Monto recibido", "Vuelto", "Articulo Id",
                        "Producto", "Codigo", "Categoria", "Unidad", "Cantidad", "Precio unitario",
                        "Costo unitario", "Subtotal", "Ganancia estimada"
                    },
                    rows));
        }

        [HttpGet]
        public IActionResult ExportChartExcel(DateTime? dateFrom, DateTime? dateTo, string? groupBy, string? metric, string? productMode, int? productLimit, string chart)
        {
            var vm = BuildStatistics(dateFrom, dateTo, groupBy, metric, productMode, productLimit);
            var headers = new List<string>();
            var rows = new List<IEnumerable<object?>>();
            var normalizedChart = NormalizeChart(chart);
            var fileName = $"estadisticas-{normalizedChart}-{vm.DateFrom:yyyyMMdd}-{vm.DateTo:yyyyMMdd}.xls";

            switch (normalizedChart)
            {
                case "usuarios":
                    headers.AddRange(new[] { "Usuario", "Ventas", "Monto" });
                    foreach (var item in vm.UserSales)
                    {
                        rows.Add(new object?[] { item.User, item.SalesCount, item.Amount });
                    }
                    break;

                case "periodos":
                    headers.AddRange(new[] { "Periodo", "Cantidad", "Monto" });
                    foreach (var item in vm.PeriodSeries)
                    {
                        rows.Add(new object?[] { item.Period, item.Quantity, item.Amount });
                    }
                    break;

                case "categorias":
                    headers.AddRange(new[] { "Categoria", "Cantidad", "Monto" });
                    foreach (var item in vm.CategoryDistribution)
                    {
                        rows.Add(new object?[] { item.Category, item.Quantity, item.Amount });
                    }
                    break;

                case "productos":
                    headers.AddRange(new[] { "Producto", "Categoria", "Cantidad", "Monto", "Ganancia estimada" });
                    foreach (var item in vm.TopProducts)
                    {
                        rows.Add(new object?[] { item.Product, item.Category, item.Quantity, item.Amount, item.EstimatedProfit });
                    }
                    break;

                case "stock":
                    headers.AddRange(new[] { "Producto", "Stock", "Stock minimo", "Unidad", "Estado" });
                    foreach (var item in vm.LowStockItems)
                    {
                        rows.Add(new object?[] { item.Product, item.Stock, item.StockMin, item.UnitOfMeasure, item.StatusText });
                    }
                    break;
            }

            return ExcelFile(fileName, new ExcelSheet(normalizedChart, headers, rows));
        }

        private StatisticsVM BuildStatistics(DateTime? dateFrom, DateTime? dateTo, string? groupBy, string? metric, string? productMode = null, int? productLimit = null)
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
            var selectedProductMode = NormalizeProductMode(productMode);
            var selectedProductLimit = NormalizeProductLimit(productLimit);
            var toExclusive = to.AddDays(1);

            var sales = _workContainer.Sale
                .GetAll(
                    s => s.Date >= from && s.Date < toExclusive && !s.IsVoided,
                    includeProperties: "User")
                .ToList();

            var saleDetails = _workContainer.SaleDetail
                .GetAll(
                    d => d.Sale.Date >= from && d.Sale.Date < toExclusive && !d.Sale.IsVoided,
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
                    EstimatedProfit = g.Sum(x => x.EstimatedProfit)
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

            var users = _userManager.Users
                .OrderBy(u => u.FullName)
                .ToList();

            var userSales = users
                .GroupJoin(
                    sales.Where(s => !string.IsNullOrWhiteSpace(s.UserId)),
                    user => user.Id,
                    sale => sale.UserId,
                    (user, userSaleGroup) => new StatisticsUserPointVM
                    {
                        User = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email ?? "Sin nombre",
                        SalesCount = userSaleGroup.Count(),
                        Amount = userSaleGroup.Sum(s => s.Total)
                    })
                .ToList();

            var anonymousSales = sales.Where(s => string.IsNullOrWhiteSpace(s.UserId)).ToList();

            if (anonymousSales.Any())
            {
                userSales.Add(new StatisticsUserPointVM
                {
                    User = "Sin usuario",
                    SalesCount = anonymousSales.Count,
                    Amount = anonymousSales.Sum(s => s.Total)
                });
            }

            var batchStockByArticle = _workContainer.ArticleBatch
                .GetAll(b => b.IsActive && b.Quantity > 0)
                .GroupBy(b => b.ArticleId)
                .ToDictionary(g => g.Key, g => g.Sum(b => b.Quantity));

            var lowStockItems = _workContainer.Article
                .GetAll(a => a.isActive)
                .Select(a =>
                {
                    batchStockByArticle.TryGetValue(a.Id, out var batchStock);
                    var stock = a.UsesBatches ? batchStock : a.Stock;
                    var warningMargin = a.IsSoldByWeight ? 0.5m : 5m;

                    return new
                    {
                        Article = a,
                        Stock = stock,
                        WarningMargin = warningMargin
                    };
                })
                .Where(a => a.Article.StockMin > 0 && a.Stock <= a.Article.StockMin + a.WarningMargin)
                .OrderBy(a => a.Stock <= a.Article.StockMin ? 0 : 1)
                .ThenBy(a => a.Stock - a.Article.StockMin)
                .Select(a => new StatisticsLowStockItemVM
                {
                    Product = a.Article.Name,
                    Stock = a.Stock,
                    StockMin = a.Article.StockMin,
                    UnitOfMeasure = a.Article.UnitOfMeasure,
                    Status = a.Stock <= a.Article.StockMin ? "danger" : "warning",
                    StatusText = a.Stock <= a.Article.StockMin ? "Bajo minimo" : "Cerca del minimo"
                })
                .Take(10)
                .ToList();

            return new StatisticsVM
            {
                DateFrom = from,
                DateTo = to,
                GroupBy = group,
                Metric = selectedMetric,
                ProductMode = selectedProductMode,
                ProductLimit = selectedProductLimit,
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
                TopProducts = SortProducts(productStats, selectedMetric, selectedProductMode)
                    .Take(selectedProductLimit)
                    .ToList(),
                UserSales = userSales
                    .OrderByDescending(u => selectedMetric == "amount" ? u.Amount : u.SalesCount)
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

        private static string NormalizeProductMode(string? productMode)
        {
            return productMode == "bottom" ? "bottom" : "top";
        }

        private static int NormalizeProductLimit(int? productLimit)
        {
            return productLimit == 25 ? 25 : 10;
        }

        private static IEnumerable<StatisticsProductPointVM> SortProducts(IEnumerable<StatisticsProductPointVM> products, string metric, string productMode)
        {
            return productMode == "bottom"
                ? products.OrderBy(p => metric == "amount" ? p.Amount : p.Quantity).ThenBy(p => p.Product)
                : products.OrderByDescending(p => metric == "amount" ? p.Amount : p.Quantity).ThenBy(p => p.Product);
        }

        private static string NormalizeChart(string? chart)
        {
            return chart switch
            {
                "usuarios" => "usuarios",
                "periodos" => "periodos",
                "categorias" => "categorias",
                "productos" => "productos",
                "stock" => "stock",
                _ => "productos"
            };
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

        private static string GetPaymentMethodDisplay(string paymentMethod)
        {
            return paymentMethod == "CuentaPersonal" ? "Cuenta personal" : paymentMethod;
        }

        private static FileContentResult ExcelFile(string fileName, params ExcelSheet[] sheets)
        {
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            xml.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");

            foreach (var sheet in sheets)
            {
                xml.AppendLine($"<Worksheet ss:Name=\"{EscapeXml(sheet.Name)}\"><Table>");
                xml.AppendLine("<Row>");

                foreach (var header in sheet.Headers)
                {
                    xml.AppendLine($"<Cell><Data ss:Type=\"String\">{EscapeXml(header)}</Data></Cell>");
                }

                xml.AppendLine("</Row>");

                foreach (var row in sheet.Rows)
                {
                    xml.AppendLine("<Row>");

                    foreach (var cell in row)
                    {
                        AppendCell(xml, cell);
                    }

                    xml.AppendLine("</Row>");
                }

                xml.AppendLine("</Table></Worksheet>");
            }

            xml.AppendLine("</Workbook>");

            var bytes = Encoding.UTF8.GetBytes(xml.ToString());
            return new FileContentResult(bytes, "application/vnd.ms-excel")
            {
                FileDownloadName = fileName
            };
        }

        private static void AppendCell(StringBuilder xml, object? value)
        {
            if (value is null)
            {
                xml.AppendLine("<Cell><Data ss:Type=\"String\"></Data></Cell>");
                return;
            }

            if (value is decimal or int or long or double or float)
            {
                var number = Convert.ToDecimal(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                xml.AppendLine($"<Cell><Data ss:Type=\"Number\">{number}</Data></Cell>");
                return;
            }

            xml.AppendLine($"<Cell><Data ss:Type=\"String\">{EscapeXml(value.ToString())}</Data></Cell>");
        }

        private static string EscapeXml(string? value) => SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;

        private sealed record ExcelSheet(string Name, IEnumerable<string> Headers, IEnumerable<IEnumerable<object?>> Rows);
    }
}
