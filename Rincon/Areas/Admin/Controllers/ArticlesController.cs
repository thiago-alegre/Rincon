using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;
using Rincon.Models.ViewModels;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Rincon.DataAccess.Data;
using Rincon.Utilities;

namespace Rincon.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]

    public class ArticlesController : Controller
    {
        private readonly IWorkContainer _workContainer;
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private const long MaxImageSizeBytes = 2 * 1024 * 1024;

        public ArticlesController(IWorkContainer workContainer, ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _workContainer = workContainer;
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            ArticleVM articleVM = new ArticleVM()
            {
                Article = new Article(),
                CategoryList = GetCategoryList()
            };

            if (id == null)
            {
                articleVM.PriceText = "";
                articleVM.CostText = "";
                articleVM.StockText = "";
                articleVM.StockMinText = "";

                return View(articleVM);
            }

            articleVM.Article = _workContainer.Article.Get(id.GetValueOrDefault());

            if (articleVM.Article == null)
            {
                return NotFound();
            }

            articleVM.Article.UnitOfMeasure = articleVM.Article.IsSoldByWeight ? "Kilogramo" : "Unidad";

            articleVM.PriceText = FormatDecimal(articleVM.Article.Price);
            articleVM.CostText = FormatDecimal(articleVM.Article.Cost);
            articleVM.StockText = FormatDecimal(articleVM.Article.Stock);
            articleVM.StockMinText = FormatDecimal(articleVM.Article.StockMin);

            return View(articleVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ArticleVM articleVM)
        {
            ModelState.Remove("Article.Price");
            ModelState.Remove("Article.Cost");
            ModelState.Remove("Article.Stock");
            ModelState.Remove("Article.StockMin");
            ModelState.Remove("Article.UsesBatches");

            var files = HttpContext.Request.Form.Files;
            IFormFile? uploadedFile = files.Count > 0 ? files[0] : null;

            if (!TryParseDecimal(articleVM.PriceText, out decimal parsedPrice))
            {
                ModelState.AddModelError("PriceText", "Ingrese un precio válido");
            }
            else if (parsedPrice <= 0)
            {
                ModelState.AddModelError("PriceText", "El precio debe ser mayor a 0");
            }
            else
            {
                articleVM.Article.Price = parsedPrice;
            }

            if (!TryParseDecimal(articleVM.CostText, out decimal parsedCost))
            {
                ModelState.AddModelError("CostText", "Ingrese un costo válido");
            }
            else if (parsedCost <= 0)
            {
                ModelState.AddModelError("CostText", "El costo debe ser mayor a 0");
            }
            else
            {
                articleVM.Article.Cost = parsedCost;
            }

            if (articleVM.Article.UsesBatches)
            {
                ModelState.Remove("StockText");
                ModelState.Remove("Article.ExpirationDate");
                articleVM.Article.Stock = 0;
                articleVM.Article.ExpirationDate = null;
            }
            else if (string.IsNullOrWhiteSpace(articleVM.StockText))
            {
                ModelState.AddModelError("StockText", "Ingrese el stock");
            }
            else if (!TryParseDecimal(articleVM.StockText, out decimal parsedStock))
            {
                ModelState.AddModelError("StockText", "Ingrese un stock válido");
            }
            else if (parsedStock < 0)
            {
                ModelState.AddModelError("StockText", "El stock no puede ser negativo");
            }
            else
            {
                articleVM.Article.Stock = parsedStock;
            }

            if (string.IsNullOrWhiteSpace(articleVM.StockMinText))
            {
                articleVM.Article.StockMin = 0;
            }
            else if (!TryParseDecimal(articleVM.StockMinText, out decimal parsedStockMin))
            {
                ModelState.AddModelError("StockMinText", "Ingrese un stock mínimo válido");
            }
            else if (parsedStockMin < 0)
            {
                ModelState.AddModelError("StockMinText", "El stock mínimo no puede ser negativo");
            }
            else
            {
                articleVM.Article.StockMin = parsedStockMin;
            }

            if (articleVM.Article.CategoryId == 0)
            {
                ModelState.AddModelError("Article.CategoryId", "Seleccione una categoría");
            }

            if (!articleVM.Article.IsSoldByWeight)
            {
                articleVM.Article.UnitOfMeasure = "Unidad";
            }
            else
            {
                articleVM.Article.UnitOfMeasure = "Kilogramo";
            }

            if (uploadedFile != null && uploadedFile.Length > 0 && !IsValidImageFile(uploadedFile))
            {
                ModelState.AddModelError("Article.ImageUrl", "Seleccione una imagen válida de hasta 2 MB (.jpg, .jpeg, .png o .webp)");
            }

            if (!ModelState.IsValid)
            {
                LoadArticleLists(articleVM);

                return View(articleVM);
            }

            string? oldImageUrl = null;

            if (articleVM.Article.Id != 0)
            {
                var objFromDb = _workContainer.Article.Get(articleVM.Article.Id);
                oldImageUrl = objFromDb?.ImageUrl;
            }

            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                articleVM.Article.ImageUrl = SaveArticleImage(uploadedFile);

                if (!string.IsNullOrEmpty(oldImageUrl))
                {
                    DeleteArticleImage(oldImageUrl);
                }
            }
            else if (articleVM.Article.Id != 0)
            {
                articleVM.Article.ImageUrl = oldImageUrl;
            }

            var isNewArticle = articleVM.Article.Id == 0;

            if (isNewArticle)
            {
                _workContainer.Article.Add(articleVM.Article);
            }
            else
            {
                _workContainer.Article.Update(articleVM.Article);
                TempData["success"] = "Artículo actualizado correctamente";
            }

            _workContainer.Save();

            if (isNewArticle && articleVM.Article.UsesBatches)
            {
                TempData["modalTitle"] = "Artículo creado correctamente";
                TempData["modalText"] = $"El artículo {articleVM.Article.Name} usa stock por lotes. Para poder venderlo, cargá al menos un lote con cantidad, costo y vencimiento si corresponde.";
                TempData["modalIcon"] = "success";
                TempData["modalConfirmText"] = "Crear lote ahora";
                TempData["modalCancelText"] = "Ir a artículos";
                TempData["modalConfirmUrl"] = Url.Action("Upsert", "ArticleBatches", new { area = "Admin", articleId = articleVM.Article.Id });
                TempData["modalShowCancel"] = "true";
            }
            else if (isNewArticle)
            {
                TempData["success"] = "Artículo creado correctamente";
            }

            return RedirectToAction(nameof(Index));
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            var draw = GetDataTablesInt("draw");
            var start = GetDataTablesInt("start");
            var length = GetDataTablesInt("length", 5);
            var searchValue = Request.Query["search[value]"].ToString()?.Trim();
            var orderColumn = GetDataTablesInt("order[0][column]");
            var orderDirection = Request.Query["order[0][dir]"].ToString();

            var query = _db.Articles
                .AsNoTracking()
                .Include(a => a.Category)
                .AsQueryable();

            var recordsTotal = query.Count();

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                var searchPattern = $"%{searchValue}%";

                query = query.Where(a =>
                    EF.Functions.ILike(a.Name, searchPattern) ||
                    EF.Functions.ILike(a.Code, searchPattern) ||
                    (a.Description != null && EF.Functions.ILike(a.Description, searchPattern)) ||
                    (a.Category != null && EF.Functions.ILike(a.Category.Name, searchPattern)));
            }

            var recordsFiltered = query.Count();

            query = orderColumn switch
            {
                1 => orderDirection == "asc" ? query.OrderBy(a => a.Name) : query.OrderByDescending(a => a.Name),
                2 => orderDirection == "asc" ? query.OrderBy(a => a.Code) : query.OrderByDescending(a => a.Code),
                3 => orderDirection == "asc"
                    ? query.OrderBy(a => a.Category != null ? a.Category.Name : string.Empty)
                    : query.OrderByDescending(a => a.Category != null ? a.Category.Name : string.Empty),
                4 => orderDirection == "asc" ? query.OrderBy(a => a.Price) : query.OrderByDescending(a => a.Price),
                5 => orderDirection == "asc" ? query.OrderBy(a => a.Cost) : query.OrderByDescending(a => a.Cost),
                6 => orderDirection == "asc"
                    ? query.OrderBy(a => a.UsesBatches
                        ? _db.ArticleBatches
                            .Where(b => b.ArticleId == a.Id && b.IsActive && b.Quantity > 0)
                            .Sum(b => (decimal?)b.Quantity) ?? 0
                        : a.Stock)
                    : query.OrderByDescending(a => a.UsesBatches
                        ? _db.ArticleBatches
                            .Where(b => b.ArticleId == a.Id && b.IsActive && b.Quantity > 0)
                            .Sum(b => (decimal?)b.Quantity) ?? 0
                        : a.Stock),
                7 => orderDirection == "asc"
                    ? query.OrderBy(a => a.UsesBatches
                        ? _db.ArticleBatches
                            .Where(b => b.ArticleId == a.Id && b.IsActive && b.Quantity > 0 && b.ExpirationDate.HasValue)
                            .Min(b => b.ExpirationDate)
                        : a.ExpirationDate)
                    : query.OrderByDescending(a => a.UsesBatches
                        ? _db.ArticleBatches
                            .Where(b => b.ArticleId == a.Id && b.IsActive && b.Quantity > 0 && b.ExpirationDate.HasValue)
                            .Min(b => b.ExpirationDate)
                        : a.ExpirationDate),
                8 => orderDirection == "asc" ? query.OrderBy(a => a.isActive) : query.OrderByDescending(a => a.isActive),
                _ => query.OrderBy(a => a.Name)
            };

            var articles = query
                .Skip(start)
                .Take(length)
                .Select(article => new
                {
                    article.Id,
                    article.Name,
                    article.Code,
                    article.Description,
                    article.Price,
                    article.Cost,
                    stock = article.UsesBatches
                        ? _db.ArticleBatches
                            .Where(b => b.ArticleId == article.Id && b.IsActive && b.Quantity > 0)
                            .Sum(b => (decimal?)b.Quantity) ?? 0
                        : article.Stock,
                    article.StockMin,
                    article.UsesBatches,
                    article.IsSoldByWeight,
                    article.UnitOfMeasure,
                    expirationDate = article.UsesBatches
                        ? _db.ArticleBatches
                            .Where(b => b.ArticleId == article.Id && b.IsActive && b.Quantity > 0 && b.ExpirationDate.HasValue)
                            .Min(b => b.ExpirationDate)
                        : article.ExpirationDate,
                    article.ImageUrl,
                    article.Date,
                    article.isActive,
                    article.CategoryId,
                    category = article.Category == null
                        ? null
                        : new
                        {
                            article.Category.Id,
                            article.Category.Name
                        }
                })
                .ToList();

            return Json(new
            {
                draw,
                recordsTotal,
                recordsFiltered,
                data = articles
            });
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var objFromDb = _workContainer.Article.Get(id);

            if (objFromDb == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Error al eliminar el artículo"
                });
            }

            if (!string.IsNullOrEmpty(objFromDb.ImageUrl))
            {
                DeleteArticleImage(objFromDb.ImageUrl);
            }

            _workContainer.Article.Remove(objFromDb);
            _workContainer.Save();

            return Json(new
            {
                success = true,
                message = "Artículo eliminado correctamente"
            });
        }

        #endregion

        #region PRIVATE METHODS

        private IEnumerable<SelectListItem> GetCategoryList()
        {
            return _workContainer.Category.GetAll(c => c.isActive == true)
                .Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                });
        }

        private string FormatDecimal(decimal value)
        {
            return value.ToString("#,##0.##", CultureInfo.GetCultureInfo("es-AR"));
        }

        private bool TryParseDecimal(string? value, out decimal result) => DecimalParser.TryParse(value, out result);

        private void LoadArticleLists(ArticleVM articleVM)
        {
            articleVM.CategoryList = GetCategoryList();
        }

        private bool IsValidImageFile(IFormFile file)
        {
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (file.Length <= 0 || file.Length > MaxImageSizeBytes)
            {
                return false;
            }

            if (!file.ContentType.StartsWith("image/"))
            {
                return false;
            }

            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".webp")
            {
                return false;
            }

            using var stream = file.OpenReadStream();
            Span<byte> header = stackalloc byte[12];
            var bytesRead = stream.Read(header);

            if (bytesRead < 4)
            {
                return false;
            }

            var isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            var isPng = bytesRead >= 8
                && header[0] == 0x89
                && header[1] == 0x50
                && header[2] == 0x4E
                && header[3] == 0x47
                && header[4] == 0x0D
                && header[5] == 0x0A
                && header[6] == 0x1A
                && header[7] == 0x0A;
            var isWebp = bytesRead >= 12
                && header[0] == 0x52
                && header[1] == 0x49
                && header[2] == 0x46
                && header[3] == 0x46
                && header[8] == 0x57
                && header[9] == 0x45
                && header[10] == 0x42
                && header[11] == 0x50;

            return extension switch
            {
                ".jpg" or ".jpeg" => isJpeg,
                ".png" => isPng,
                ".webp" => isWebp,
                _ => false
            };
        }

        private string SaveArticleImage(IFormFile file)
        {
            string fileName = Guid.NewGuid().ToString();
            string extension = Path.GetExtension(file.FileName);
            string uploads = Path.Combine(_webHostEnvironment.WebRootPath, @"imagenes\articles");

            if (!Directory.Exists(uploads))
            {
                Directory.CreateDirectory(uploads);
            }

            using (var fileStream = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            return @"\imagenes\articles\" + fileName + extension;
        }

        private void DeleteArticleImage(string imageUrl)
        {
            string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('\\', '/'));

            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }

        private int GetDataTablesInt(string key, int defaultValue = 0)
        {
            return int.TryParse(Request.Query[key], out var value) ? value : defaultValue;
        }

        #endregion
    }
}
