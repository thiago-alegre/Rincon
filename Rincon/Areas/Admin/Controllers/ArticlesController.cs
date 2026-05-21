using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;
using Rincon.Models.ViewModels;
using System.Globalization;

namespace Rincon.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ArticlesController : Controller
    {
        private readonly IWorkContainer _workContainer;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ArticlesController(IWorkContainer workContainer, IWebHostEnvironment webHostEnvironment)
        {
            _workContainer = workContainer;
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

            if (!TryParseDecimal(articleVM.StockText, out decimal parsedStock))
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
                ModelState.AddModelError("Article.ImageUrl", "Seleccione una imagen válida (.jpg, .jpeg, .png o .webp)");
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

            if (articleVM.Article.Id == 0)
            {
                _workContainer.Article.Add(articleVM.Article);
                TempData["success"] = "Artículo creado correctamente";
            }
            else
            {
                _workContainer.Article.Update(articleVM.Article);
                TempData["success"] = "Artículo actualizado correctamente";
            }

            _workContainer.Save();

            return RedirectToAction(nameof(Index));
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            return Json(new
            {
                data = _workContainer.Article.GetAll(includeProperties: "Category")
            });
        }

        [HttpDelete]
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

        private bool TryParseDecimal(string? value, out decimal result)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            value = value.Trim();
            value = value.Replace("$", "");
            value = value.Replace(" ", "");

            bool hasComma = value.Contains(",");
            bool hasDot = value.Contains(".");

            if (!hasComma && !hasDot)
            {
                return decimal.TryParse(
                    value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out result
                );
            }

            if (hasComma && !hasDot)
            {
                int commaCount = value.Split(',').Length - 1;
                int lastComma = value.LastIndexOf(",");
                int digitsAfterComma = value.Length - lastComma - 1;

                if (commaCount == 1 && digitsAfterComma == 3)
                {
                    value = value.Replace(",", "");
                }
                else
                {
                    value = value.Replace(",", ".");
                }

                return decimal.TryParse(
                    value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out result
                );
            }

            if (hasDot && !hasComma)
            {
                int lastDot = value.LastIndexOf(".");
                int digitsAfterDot = value.Length - lastDot - 1;

                if (digitsAfterDot == 3)
                {
                    value = value.Replace(".", "");
                }

                return decimal.TryParse(
                    value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out result
                );
            }

            if (hasComma && hasDot)
            {
                int lastComma = value.LastIndexOf(",");
                int lastDot = value.LastIndexOf(".");

                if (lastComma > lastDot)
                {
                    value = value.Replace(".", "");
                    value = value.Replace(",", ".");
                }
                else
                {
                    value = value.Replace(",", "");
                }

                return decimal.TryParse(
                    value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out result
                );
            }

            return false;
        }

        private void LoadArticleLists(ArticleVM articleVM)
        {
            articleVM.CategoryList = GetCategoryList();
        }

        private bool IsValidImageFile(IFormFile file)
        {
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            return file.ContentType.StartsWith("image/")
                && (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".webp");
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

        #endregion
    }
}
