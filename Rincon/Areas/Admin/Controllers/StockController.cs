using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rincon.DataAccess.Data;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Utilities;

namespace Rincon.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class StockController : Controller
    {
        private readonly IWorkContainer _workContainer;
        private readonly ApplicationDbContext _db;

        public StockController(IWorkContainer workContainer, ApplicationDbContext db)
        {
            _workContainer = workContainer;
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var draw = GetDataTablesInt("draw");
            var start = GetDataTablesInt("start");
            var length = GetDataTablesInt("length", 5);
            var searchValue = Request.Query["search[value]"].ToString()?.Trim();
            var orderColumn = GetDataTablesInt("order[0][column]");
            var orderDirection = Request.Query["order[0][dir]"].ToString();
            var today = DateTime.Today;

            var query = _db.Articles
                .AsNoTracking()
                .Include(a => a.Category)
                .Where(a => a.isActive && a.UsesBatches)
                .Select(a => new
                {
                    Article = a,
                    CategoryName = a.Category != null ? a.Category.Name : "Sin categoría",
                    Stock = _db.ArticleBatches
                        .Where(b => b.ArticleId == a.Id && b.IsActive && b.Quantity > 0)
                        .Select(b => (decimal?)b.Quantity)
                        .Sum() ?? 0,
                    BatchCount = _db.ArticleBatches
                        .Count(b => b.ArticleId == a.Id && b.IsActive),
                    NearestExpiration = _db.ArticleBatches
                        .Where(b => b.ArticleId == a.Id && b.IsActive && b.Quantity > 0 && b.ExpirationDate.HasValue)
                        .OrderBy(b => b.ExpirationDate)
                        .Select(b => b.ExpirationDate)
                        .FirstOrDefault()
                });

            var recordsTotal = query.Count();

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                var searchPattern = $"%{searchValue}%";

                query = query.Where(x =>
                    EF.Functions.ILike(x.Article.Name, searchPattern) ||
                    EF.Functions.ILike(x.Article.Code, searchPattern) ||
                    EF.Functions.ILike(x.CategoryName, searchPattern));
            }

            var recordsFiltered = query.Count();

            query = orderColumn switch
            {
                0 => orderDirection == "desc" ? query.OrderByDescending(x => x.Article.Name) : query.OrderBy(x => x.Article.Name),
                1 => orderDirection == "desc" ? query.OrderByDescending(x => x.CategoryName) : query.OrderBy(x => x.CategoryName),
                2 => orderDirection == "desc" ? query.OrderByDescending(x => x.Stock) : query.OrderBy(x => x.Stock),
                3 => orderDirection == "desc" ? query.OrderByDescending(x => x.Article.StockMin) : query.OrderBy(x => x.Article.StockMin),
                4 => orderDirection == "desc" ? query.OrderByDescending(x => x.BatchCount) : query.OrderBy(x => x.BatchCount),
                5 => orderDirection == "desc" ? query.OrderByDescending(x => x.NearestExpiration) : query.OrderBy(x => x.NearestExpiration),
                _ => query.OrderBy(x => x.Article.Name)
            };

            var data = query
                .Skip(start)
                .Take(length)
                .ToList()
                .Select(x =>
                {
                    var stockStatus = GetStockStatus(x.Stock, x.Article.StockMin, x.Article.IsSoldByWeight);
                    var status = x.BatchCount == 0 ? "empty" : stockStatus;
                    var expirationStatus = "empty";

                    if (x.NearestExpiration.HasValue)
                    {
                        var days = (x.NearestExpiration.Value.Date - today).Days;
                        expirationStatus = days < 0 ? "danger" : days <= 10 ? "warning" : "ok";
                    }

                    return new
                    {
                        x.Article.Id,
                        product = x.Article.Name,
                        category = x.CategoryName,
                        stock = x.Stock,
                        x.Article.StockMin,
                        x.Article.UnitOfMeasure,
                        x.Article.IsSoldByWeight,
                        batchCount = x.BatchCount,
                        expirationDate = x.NearestExpiration?.ToString("yyyy-MM-dd"),
                        expirationDisplay = x.NearestExpiration?.ToString("dd/MM/yyyy") ?? "Sin vencimiento",
                        stockStatus,
                        expirationStatus,
                        status,
                        statusDisplay = GetStatusDisplay(status),
                        detailUrl = Url.Action("Index", "ArticleBatches", new { area = "Admin", articleId = x.Article.Id })
                    };
                });

            return Json(new
            {
                draw,
                recordsTotal,
                recordsFiltered,
                data
            });
        }

        [HttpGet]
        public IActionResult SearchBatchArticles(string? term, int page = 1)
        {
            const int pageSize = 10;

            page = page < 1 ? 1 : page;

            var articles = _workContainer.Article
                .GetAll(a => a.isActive, includeProperties: "Category");

            if (!string.IsNullOrWhiteSpace(term))
            {
                articles = articles.Where(a =>
                    a.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    a.Code.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (a.Category != null && a.Category.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            var pagedArticles = articles
                .OrderBy(a => a.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize + 1)
                .ToList();

            return Json(new
            {
                results = pagedArticles.Take(pageSize).Select(a => new
                {
                    id = a.Id,
                    text = $"{a.Name} - {a.Code}",
                    category = a.Category?.Name ?? "Sin categoría",
                    usesBatches = a.UsesBatches
                }),
                pagination = new
                {
                    more = pagedArticles.Count > pageSize
                }
            });
        }

        private static string GetStockStatus(decimal stock, decimal stockMin, bool isSoldByWeight)
        {
            if (stock <= stockMin)
            {
                return "danger";
            }

            if (stockMin <= 0)
            {
                return "ok";
            }

            var warningMargin = isSoldByWeight ? 0.5m : 5m;

            return stock <= stockMin + warningMargin ? "warning" : "ok";
        }

        private static string GetStatusDisplay(string status)
        {
            return status switch
            {
                "danger" => "Bajo mínimo",
                "warning" => "Cerca del mínimo",
                "ok" => "Disponible",
                _ => "Sin lotes cargados"
            };
        }

        private int GetDataTablesInt(string key, int defaultValue = 0)
        {
            return int.TryParse(Request.Query[key], out var value) ? value : defaultValue;
        }
    }
}
