using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rincon.DataAccess.Data;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;
using Rincon.Utilities;

namespace Rincon.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ArticleBatchesController : Controller
    {
        private readonly IWorkContainer _workContainer;
        private readonly ApplicationDbContext _db;

        public ArticleBatchesController(IWorkContainer workContainer, ApplicationDbContext db)
        {
            _workContainer = workContainer;
            _db = db;
        }

        public IActionResult Index(int articleId)
        {
            var article = _workContainer.Article.Get(articleId);

            if (article == null)
            {
                return NotFound();
            }

            ViewBag.Article = article;
            return View();
        }

        public IActionResult Upsert(int articleId, int? id)
        {
            var article = _workContainer.Article.Get(articleId);

            if (article == null)
            {
                return NotFound();
            }

            ViewBag.Article = article;

            if (id == null || id == 0)
            {
                return View(new ArticleBatch
                {
                    ArticleId = articleId,
                    PurchaseDate = DateTime.Today,
                    Cost = article.Cost
                });
            }

            var batch = _workContainer.ArticleBatch.Get(id.Value);

            if (batch == null || batch.ArticleId != articleId)
            {
                return NotFound();
            }

            return View(batch);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ArticleBatch batch, string? quantityText, string? initialQuantityText, string? costText)
        {
            var article = _workContainer.Article.Get(batch.ArticleId);

            if (article == null)
            {
                return NotFound();
            }

            if (!DecimalParser.TryParse(quantityText, out decimal quantity) || quantity < 0)
            {
                ModelState.AddModelError("Quantity", "Ingrese una cantidad disponible válida");
            }

            if (batch.Id == 0 || string.IsNullOrWhiteSpace(initialQuantityText))
            {
                initialQuantityText = quantityText;
            }

            if (!DecimalParser.TryParse(initialQuantityText, out decimal initialQuantity) || initialQuantity <= 0)
            {
                ModelState.AddModelError("InitialQuantity", "Ingrese una cantidad inicial válida");
            }

            if (!DecimalParser.TryParse(costText, out decimal cost) || cost < 0)
            {
                ModelState.AddModelError("Cost", "Ingrese un costo válido");
            }

            if (quantity > initialQuantity)
            {
                ModelState.AddModelError("Quantity", "La cantidad disponible no puede superar la cantidad inicial");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Article = article;
                return View(batch);
            }

            batch.Quantity = quantity;
            batch.InitialQuantity = initialQuantity;
            batch.Cost = cost;

            if (batch.Id == 0)
            {
                batch.CreatedAt = DateTime.Now;
                _workContainer.ArticleBatch.Add(batch);
                TempData["success"] = "Lote creado correctamente";
            }
            else
            {
                _workContainer.ArticleBatch.Update(batch);
                TempData["success"] = "Lote actualizado correctamente";
            }

            _workContainer.Save();
            SyncArticleStock(batch.ArticleId);

            return RedirectToAction(nameof(Index), new { articleId = batch.ArticleId });
        }

        [HttpGet]
        public IActionResult GetAll(int articleId)
        {
            var draw = GetDataTablesInt("draw");
            var start = GetDataTablesInt("start");
            var length = GetDataTablesInt("length", 5);
            var searchValue = Request.Query["search[value]"].ToString()?.Trim();
            var orderColumn = GetDataTablesInt("order[0][column]");
            var orderDirection = Request.Query["order[0][dir]"].ToString();

            var query = _db.ArticleBatches
                .AsNoTracking()
                .Where(b => b.ArticleId == articleId)
                .AsQueryable();

            var recordsTotal = query.Count();

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                var isActiveSearch = "activo".Contains(searchValue, StringComparison.OrdinalIgnoreCase);
                var isInactiveSearch = "inactivo".Contains(searchValue, StringComparison.OrdinalIgnoreCase);

                query = query.Where(b =>
                    b.Id.ToString().Contains(searchValue) ||
                    (isActiveSearch && b.IsActive) ||
                    (isInactiveSearch && !b.IsActive));
            }

            var recordsFiltered = query.Count();

            query = orderColumn switch
            {
                0 => orderDirection == "desc" ? query.OrderByDescending(b => b.PurchaseDate) : query.OrderBy(b => b.PurchaseDate),
                1 => orderDirection == "desc" ? query.OrderByDescending(b => b.ExpirationDate) : query.OrderBy(b => b.ExpirationDate),
                2 => orderDirection == "desc" ? query.OrderByDescending(b => b.Quantity) : query.OrderBy(b => b.Quantity),
                3 => orderDirection == "desc" ? query.OrderByDescending(b => b.InitialQuantity) : query.OrderBy(b => b.InitialQuantity),
                4 => orderDirection == "desc" ? query.OrderByDescending(b => b.Cost) : query.OrderBy(b => b.Cost),
                5 => orderDirection == "desc" ? query.OrderByDescending(b => b.IsActive) : query.OrderBy(b => b.IsActive),
                _ => query.OrderBy(b => b.ExpirationDate ?? DateTime.MaxValue).ThenBy(b => b.PurchaseDate)
            };

            var data = query
                .Skip(start)
                .Take(length)
                .ToList()
                .Select(b => new
                {
                    b.Id,
                    b.ArticleId,
                    b.Quantity,
                    b.InitialQuantity,
                    b.Cost,
                    b.IsActive,
                    purchaseDate = b.PurchaseDate.ToString("yyyy-MM-dd"),
                    expirationDate = b.ExpirationDate?.ToString("yyyy-MM-dd"),
                    expirationDisplay = b.ExpirationDate?.ToString("dd/MM/yyyy") ?? "Sin vencimiento"
                });

            return Json(new
            {
                draw,
                recordsTotal,
                recordsFiltered,
                data
            });
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var batch = _workContainer.ArticleBatch.Get(id);

            if (batch == null)
            {
                return Json(new { success = false, message = "No se encontró el lote" });
            }

            batch.IsActive = false;
            _workContainer.ArticleBatch.Update(batch);
            _workContainer.Save();
            SyncArticleStock(batch.ArticleId);

            return Json(new { success = true, message = "Lote desactivado correctamente" });
        }

        private void SyncArticleStock(int articleId)
        {
            var article = _workContainer.Article.Get(articleId);

            if (article == null)
            {
                return;
            }

            article.UsesBatches = true;
            article.Stock = _workContainer.ArticleBatch
                .GetAll(b => b.ArticleId == articleId && b.IsActive && b.Quantity > 0)
                .Sum(b => b.Quantity);

            _workContainer.Article.Update(article);
            _workContainer.Save();
        }

        private int GetDataTablesInt(string key, int defaultValue = 0)
        {
            return int.TryParse(Request.Query[key], out var value) ? value : defaultValue;
        }
    }
}
