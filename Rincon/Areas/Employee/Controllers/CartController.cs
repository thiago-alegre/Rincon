using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rincon.DataAccess.Data;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Extensions;
using Rincon.Models;
using Rincon.Models.ViewModels;
using Rincon.Utilities;
using Rincon.Utilities.Enums;
using System.Globalization;
using System.Security.Claims;

namespace Rincon.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
    public class CartController : Controller
    {
        private const string SessionCart = "SessionShoppingCart";
        private readonly IWorkContainer _workContainer;
        private readonly ApplicationDbContext _db;

        public CartController(IWorkContainer workContainer, ApplicationDbContext db)
        {
            _workContainer = workContainer;
            _db = db;
        }

        public IActionResult Index(string? searchString)
        {
            ViewBag.CurrentSearch = searchString;
            return View(BuildCartVM(searchString));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int articleId, string? quantityText)
        {
            var article = _workContainer.Article.Get(articleId);

            if (article == null || !article.isActive)
            {
                TempData["error"] = "El articulo no esta disponible";
                return RedirectToAction(nameof(Index));
            }

            if (!TryParseDecimal(quantityText, out decimal quantity) || quantity <= 0)
            {
                TempData["error"] = "Ingrese una cantidad valida";
                return RedirectToAction(nameof(Index));
            }

            if (!TryAddArticle(article, quantity, out string message))
            {
                TempData["error"] = message;
                return RedirectToAction(nameof(Index));
            }

            TempData["success"] = "Articulo agregado al carrito";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddByCode(string? codeText, string? quantityText)
        {
            if (string.IsNullOrWhiteSpace(codeText))
            {
                TempData["error"] = "Ingrese o escanee un codigo";
                return RedirectToAction(nameof(Index));
            }

            string code = codeText.Trim();
            var article = _workContainer.Article.GetFirstOrDefault(a => a.Code == code && a.isActive == true);

            if (article == null)
            {
                TempData["error"] = "No se encontro un producto activo con ese codigo";
                return RedirectToAction(nameof(Index), new { searchString = code });
            }

            string quantityToAdd = string.IsNullOrWhiteSpace(quantityText)
                ? GetDefaultIncrement(article).ToString(CultureInfo.InvariantCulture)
                : quantityText;

            if (!TryParseDecimal(quantityToAdd, out decimal quantity) || quantity <= 0)
            {
                TempData["error"] = "Ingrese una cantidad valida";
                return RedirectToAction(nameof(Index));
            }

            if (!TryAddArticle(article, quantity, out string message))
            {
                TempData["error"] = message;
                return RedirectToAction(nameof(Index));
            }

            TempData["success"] = "Articulo agregado al carrito";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddManual(string? manualName, string? manualAmountText)
        {
            if (!TryParseDecimal(manualAmountText, out decimal amount) || amount <= 0)
            {
                TempData["error"] = "Ingrese un importe valido para la venta manual";
                return RedirectToAction(nameof(Index));
            }

            var cart = GetCart();
            cart.Add(new ShoppingCartItemVM
            {
                LineId = Guid.NewGuid().ToString("N"),
                IsManual = true,
                ManualName = string.IsNullOrWhiteSpace(manualName) ? "Producto suelto" : manualName.Trim(),
                ManualUnitPrice = amount,
                Quantity = 1,
                UnitOfMeasure = "Unidad"
            });

            SaveCart(cart);

            TempData["success"] = "Producto suelto agregado al carrito";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Plus(int articleId)
        {
            var article = _workContainer.Article.Get(articleId);

            if (article == null)
            {
                TempData["error"] = "El articulo no esta disponible";
                return RedirectToAction(nameof(Index));
            }

            decimal increment = GetDefaultIncrement(article);

            if (!TryAddArticle(article, increment, out string message))
            {
                TempData["error"] = message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Minus(int articleId)
        {
            var article = _workContainer.Article.Get(articleId);
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => !i.IsManual && i.ArticleId == articleId);

            if (article == null || item == null)
            {
                return RedirectToAction(nameof(Index));
            }

            item.Quantity -= GetDefaultIncrement(article);

            if (item.Quantity <= 0)
            {
                cart.Remove(item);
            }

            SaveCart(cart);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int articleId, string? quantityText)
        {
            var article = _workContainer.Article.Get(articleId);
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => !i.IsManual && i.ArticleId == articleId);

            if (article == null || item == null)
            {
                TempData["error"] = "El articulo no esta disponible";
                return RedirectToAction(nameof(Index));
            }

            if (!TryParseDecimal(quantityText, out decimal quantity) || quantity <= 0)
            {
                TempData["error"] = "Ingrese una cantidad valida";
                return RedirectToAction(nameof(Index));
            }

            if (!IsValidQuantityForArticle(article, quantity))
            {
                TempData["error"] = "Los productos por unidad deben agregarse en cantidades enteras";
                return RedirectToAction(nameof(Index));
            }

            var availableStock = GetAvailableStock(article);

            if (quantity > availableStock)
            {
                TempData["error"] = "La cantidad supera el stock disponible";
                return RedirectToAction(nameof(Index));
            }

            item.Quantity = quantity;
            SaveCart(cart);

            TempData["success"] = "Cantidad actualizada";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int? articleId, string? lineId)
        {
            var cart = GetCart();
            var item = !string.IsNullOrWhiteSpace(lineId)
                ? cart.FirstOrDefault(i => i.LineId == lineId)
                : cart.FirstOrDefault(i => !i.IsManual && i.ArticleId == articleId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            SaveCart(new List<ShoppingCartItemVM>());
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult GetCartCount()
        {
            return Json(new
            {
                count = GetCart().Count
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmSale(PaymentMethod paymentMethod, string? amountReceivedText, int? personalAccountId)
        {
            var cart = GetCart();

            if (!cart.Any())
            {
                TempData["error"] = "El carrito está vacío";
                return RedirectToAction(nameof(Index));
            }

            decimal total = 0;

            foreach (var cartItem in cart)
            {
                if (cartItem.IsManual)
                {
                    if (cartItem.Quantity <= 0 || cartItem.ManualUnitPrice <= 0)
                    {
                        TempData["error"] = $"El importe de {cartItem.ManualName ?? "Producto suelto"} no es valido";
                        return RedirectToAction(nameof(Index));
                    }

                    total += cartItem.ManualUnitPrice * cartItem.Quantity;
                    continue;
                }

                if (!cartItem.ArticleId.HasValue)
                {
                    TempData["error"] = "Uno de los artículos del carrito no es válido";
                    return RedirectToAction(nameof(Index));
                }

                var article = _workContainer.Article.Get(cartItem.ArticleId.Value);

                if (article == null || !article.isActive)
                {
                    TempData["error"] = "Uno de los artículos del carrito ya no está disponible";
                    return RedirectToAction(nameof(Index));
                }

                if (cartItem.Quantity <= 0)
                {
                    TempData["error"] = $"La cantidad de {article.Name} no es válida";
                    return RedirectToAction(nameof(Index));
                }

                if (!IsValidQuantityForArticle(article, cartItem.Quantity))
                {
                    TempData["error"] = $"El artículo {article.Name} se vende por unidad. La cantidad debe ser entera";
                    return RedirectToAction(nameof(Index));
                }

                var availableStock = GetAvailableStock(article);

                if (cartItem.Quantity > availableStock)
                {
                    TempData["error"] = $"Stock insuficiente para {article.Name}. Stock disponible: {availableStock} {article.UnitOfMeasure}";
                    return RedirectToAction(nameof(Index));
                }

                total += article.Price * cartItem.Quantity;
            }

            decimal? amountReceived = null;
            decimal? change = null;

            if (paymentMethod == PaymentMethod.CuentaPersonal)
            {
                if (!personalAccountId.HasValue || personalAccountId.Value <= 0)
                {
                    TempData["error"] = "Seleccione una cuenta personal";
                    return RedirectToAction(nameof(Index));
                }

                var personalAccount = _workContainer.PersonalAccount.GetFirstOrDefault(a => a.Id == personalAccountId.Value && a.isActive);

                if (personalAccount == null)
                {
                    TempData["error"] = "La cuenta personal seleccionada no está disponible";
                    return RedirectToAction(nameof(Index));
                }
            }

            if (!string.IsNullOrWhiteSpace(amountReceivedText))
            {
                if (TryParseDecimal(amountReceivedText, out decimal parsedAmountReceived))
                {
                    amountReceived = parsedAmountReceived;
                    change = parsedAmountReceived - total;
                }
            }

            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var openCashRegister = _workContainer.CashRegisterSession.GetFirstOrDefault(
                s => s.UserId == userId && s.ClosedAt == null);

            if (openCashRegister == null)
            {
                TempData["error"] = "Debe abrir una caja antes de confirmar ventas";
                return RedirectToAction(nameof(Index));
            }

            using var transaction = _db.Database.BeginTransaction();

            try
            {
                var sale = new Sale
                {
                    Date = DateTime.Now,
                    Total = total,
                    PaymentMethod = paymentMethod,
                    AmountReceived = amountReceived,
                    Change = change,
                    UserId = userId,
                    CashRegisterSessionId = openCashRegister.Id,
                    PersonalAccountId = paymentMethod == PaymentMethod.CuentaPersonal ? personalAccountId : null,
                    IsPersonalAccountSettled = paymentMethod != PaymentMethod.CuentaPersonal
                };

                _workContainer.Sale.Add(sale);
                _workContainer.Save();

                foreach (var cartItem in cart)
                {
                    if (cartItem.IsManual)
                    {
                        var manualSubtotal = cartItem.ManualUnitPrice * cartItem.Quantity;
                        var manualSaleDetail = new SaleDetail
                        {
                            SaleId = sale.Id,
                            ArticleId = null,
                            ArticleName = string.IsNullOrWhiteSpace(cartItem.ManualName) ? "Producto suelto" : cartItem.ManualName,
                            ArticleCode = "MANUAL",
                            Quantity = cartItem.Quantity,
                            UnitPrice = cartItem.ManualUnitPrice,
                            UnitCost = 0,
                            Subtotal = manualSubtotal,
                            EstimatedProfit = 0,
                            UnitOfMeasure = cartItem.UnitOfMeasure
                        };

                        _workContainer.SaleDetail.Add(manualSaleDetail);
                        continue;
                    }

                    if (!cartItem.ArticleId.HasValue)
                    {
                        throw new InvalidOperationException("Artículo inválido en el carrito");
                    }

                    var article = _workContainer.Article.Get(cartItem.ArticleId.Value);

                    if (article == null)
                    {
                        throw new InvalidOperationException("Artículo no disponible al procesar la venta");
                    }

                    var subtotal = article.Price * cartItem.Quantity;
                    var unitCost = DeductArticleStock(article, cartItem.Quantity, out var batchConsumptions);
                    var estimatedProfit = (article.Price - unitCost) * cartItem.Quantity;

                    var saleDetail = new SaleDetail
                    {
                        SaleId = sale.Id,
                        ArticleId = article.Id,
                        ArticleName = article.Name,
                        ArticleCode = article.Code,
                        Quantity = cartItem.Quantity,
                        UnitPrice = article.Price,
                        UnitCost = unitCost,
                        Subtotal = subtotal,
                        EstimatedProfit = estimatedProfit,
                        UnitOfMeasure = article.UnitOfMeasure
                    };

                    _workContainer.SaleDetail.Add(saleDetail);
                    _workContainer.Save();

                    foreach (var batchConsumption in batchConsumptions)
                    {
                        _workContainer.SaleDetailBatch.Add(new SaleDetailBatch
                        {
                            SaleDetailId = saleDetail.Id,
                            ArticleBatchId = batchConsumption.ArticleBatchId,
                            Quantity = batchConsumption.Quantity,
                            UnitCost = batchConsumption.UnitCost
                        });
                    }
                }

                _workContainer.Save();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                TempData["error"] = "No se pudo registrar la venta. No se modificó el stock ni se guardó la venta.";
                return RedirectToAction(nameof(Index));
            }
            SaveCart(new List<ShoppingCartItemVM>());

            TempData["saleSuccess"] = "Venta registrada correctamente";

            return RedirectToAction(nameof(Index));
        }

        private bool TryAddArticle(Article article, decimal quantity, out string message)
        {
            message = "";

            if (!IsValidQuantityForArticle(article, quantity))
            {
                message = "Los productos por unidad deben agregarse en cantidades enteras";
                return false;
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(i => !i.IsManual && i.ArticleId == article.Id);
            decimal quantityInCart = item?.Quantity ?? 0;

            var availableStock = GetAvailableStock(article);

            if (quantityInCart + quantity > availableStock)
            {
                message = "La cantidad supera el stock disponible";
                return false;
            }

            if (item == null)
            {
                cart.Add(new ShoppingCartItemVM
                {
                    ArticleId = article.Id,
                    IsManual = false,
                    Quantity = quantity
                });
            }
            else
            {
                item.Quantity += quantity;
            }

            SaveCart(cart);
            return true;
        }

        private CartVM BuildCartVM(string? searchString = null)
        {
            var items = new List<CartItemVM>();

            foreach (var cartItem in GetCart())
            {
                if (cartItem.IsManual)
                {
                    items.Add(new CartItemVM
                    {
                        LineId = cartItem.LineId,
                        IsManual = true,
                        ManualName = cartItem.ManualName,
                        ManualUnitPrice = cartItem.ManualUnitPrice,
                        Quantity = cartItem.Quantity,
                        UnitOfMeasure = cartItem.UnitOfMeasure
                    });

                    continue;
                }

                if (!cartItem.ArticleId.HasValue)
                {
                    continue;
                }

                var article = _workContainer.Article.Get(cartItem.ArticleId.Value);

                if (article == null || !article.isActive)
                {
                    continue;
                }

                article.Stock = GetAvailableStock(article);

                items.Add(new CartItemVM
                {
                    LineId = cartItem.LineId,
                    Article = article,
                    Quantity = cartItem.Quantity
                });
            }

            return new CartVM
            {
                Items = items,
                SearchString = searchString,
                SearchResults = GetSearchResults(searchString)
            };
        }

        private IEnumerable<Article> GetSearchResults(string? searchString)
        {
            if (string.IsNullOrWhiteSpace(searchString))
            {
                return new List<Article>();
            }

            string normalizedSearch = searchString.Trim();
            string likeSearch = $"%{normalizedSearch}%";

            var articles = _db.Articles
                .AsNoTracking()
                .Include(a => a.Category)
                .Where(a => a.isActive == true)
                .Where(a =>
                    EF.Functions.ILike(a.Name, likeSearch) ||
                    EF.Functions.ILike(a.Code, likeSearch) ||
                    (a.Category != null && EF.Functions.ILike(a.Category.Name, likeSearch)))
                .OrderBy(a => a.Name)
                .Take(25)
                .ToList();

            foreach (var article in articles)
            {
                article.Stock = GetAvailableStock(article);
            }

            return articles;
        }
        private List<ShoppingCartItemVM> GetCart()
        {
            return HttpContext.Session.GetObject<List<ShoppingCartItemVM>>(SessionCart)
                ?? new List<ShoppingCartItemVM>();
        }

        private void SaveCart(List<ShoppingCartItemVM> cart)
        {
            HttpContext.Session.SetObject(SessionCart, cart);
        }

        private decimal GetAvailableStock(Article article)
        {
            if (!article.UsesBatches)
            {
                return article.Stock;
            }

            var batchStock = _workContainer.ArticleBatch
                .GetAll(b => b.ArticleId == article.Id && b.IsActive && b.Quantity > 0)
                .Sum(b => b.Quantity);

            return batchStock;
        }

        private decimal DeductArticleStock(Article article, decimal quantity, out List<BatchConsumption> batchConsumptions)
        {
            batchConsumptions = new List<BatchConsumption>();

            if (!article.UsesBatches)
            {
                article.Stock -= quantity;
                _workContainer.Article.Update(article);
                return article.Cost;
            }

            var batches = _workContainer.ArticleBatch
                .GetAll(b => b.ArticleId == article.Id && b.IsActive && b.Quantity > 0)
                .OrderBy(b => b.ExpirationDate ?? DateTime.MaxValue)
                .ThenBy(b => b.PurchaseDate)
                .ToList();

            var availableStock = batches.Sum(b => b.Quantity);

            if (availableStock < quantity)
            {
                throw new InvalidOperationException("Stock insuficiente en lotes");
            }

            var remaining = quantity;
            decimal consumedCost = 0;

            foreach (var batch in batches)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var consumed = Math.Min(batch.Quantity, remaining);
                batch.Quantity -= consumed;
                remaining -= consumed;
                consumedCost += consumed * batch.Cost;
                batchConsumptions.Add(new BatchConsumption(batch.Id, consumed, batch.Cost));

                _workContainer.ArticleBatch.Update(batch);
            }

            article.Stock = batches.Sum(b => b.Quantity);
            _workContainer.Article.Update(article);

            return quantity > 0 ? consumedCost / quantity : article.Cost;
        }

        private sealed record BatchConsumption(int ArticleBatchId, decimal Quantity, decimal UnitCost);

        private decimal GetDefaultIncrement(Article article)
        {
            if (!article.IsSoldByWeight || article.UnitOfMeasure == "Unidad")
            {
                return 1;
            }

            return 0.1m;
        }

        private bool IsValidQuantityForArticle(Article article, decimal quantity)
        {
            if (!article.IsSoldByWeight || article.UnitOfMeasure == "Unidad")
            {
                return decimal.Truncate(quantity) == quantity;
            }

            return true;
        }

        private bool TryParseDecimal(string? value, out decimal result) => DecimalParser.TryParse(value, out result);
    }
}

