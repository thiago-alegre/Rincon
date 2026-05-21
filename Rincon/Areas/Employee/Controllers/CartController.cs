using Microsoft.AspNetCore.Mvc;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Extensions;
using Rincon.Models;
using Rincon.Models.ViewModels;
using System.Globalization;
using Rincon.Utilities.Enums;
using System.Security.Claims;

namespace Rincon.Areas.Employee.Controllers
{
    [Area("Employee")]
    public class CartController : Controller
    {
        private const string SessionCart = "SessionShoppingCart";
        private readonly IWorkContainer _workContainer;

        public CartController(IWorkContainer workContainer)
        {
            _workContainer = workContainer;
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
            var item = cart.FirstOrDefault(i => i.ArticleId == articleId);

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
            var item = cart.FirstOrDefault(i => i.ArticleId == articleId);

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

            if (quantity > article.Stock)
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
        public IActionResult Remove(int articleId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.ArticleId == articleId);

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
        public IActionResult ConfirmSale(PaymentMethod paymentMethod, string? amountReceivedText)
        {
            var cart = GetCart();

            if (!cart.Any())
            {
                TempData["error"] = "El carrito está vacío";
                return RedirectToAction(nameof(Index));
            }

            if (paymentMethod == PaymentMethod.CuentaPersonal)
            {
                TempData["error"] = "Cuenta personal todavía no está implementada";
                return RedirectToAction(nameof(Index));
            }

            decimal total = 0;

            foreach (var cartItem in cart)
            {
                var article = _workContainer.Article.Get(cartItem.ArticleId);

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

                if (cartItem.Quantity > article.Stock)
                {
                    TempData["error"] = $"Stock insuficiente para {article.Name}. Stock disponible: {article.Stock} {article.UnitOfMeasure}";
                    return RedirectToAction(nameof(Index));
                }

                total += article.Price * cartItem.Quantity;
            }

            decimal? amountReceived = null;
            decimal? change = null;

            if (paymentMethod == PaymentMethod.Efectivo)
            {
                if (!TryParseDecimal(amountReceivedText, out decimal parsedAmountReceived))
                {
                    TempData["error"] = "Ingrese un monto recibido válido";
                    return RedirectToAction(nameof(Index));
                }

                if (parsedAmountReceived < total)
                {
                    TempData["error"] = "El monto recibido es insuficiente";
                    return RedirectToAction(nameof(Index));
                }

                amountReceived = parsedAmountReceived;
                change = parsedAmountReceived - total;
            }

            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var sale = new Sale
            {
                Date = DateTime.Now,
                Total = total,
                PaymentMethod = paymentMethod,
                AmountReceived = amountReceived,
                Change = change,
                UserId = userId
            };

            _workContainer.Sale.Add(sale);
            _workContainer.Save();

            foreach (var cartItem in cart)
            {
                var article = _workContainer.Article.Get(cartItem.ArticleId);

                if (article == null)
                {
                    TempData["error"] = "Ocurrió un error al procesar la venta";
                    return RedirectToAction(nameof(Index));
                }

                var saleDetail = new SaleDetail
                {
                    SaleId = sale.Id,
                    ArticleId = article.Id,
                    ArticleName = article.Name,
                    ArticleCode = article.Code,
                    Quantity = cartItem.Quantity,
                    UnitPrice = article.Price,
                    Subtotal = article.Price * cartItem.Quantity,
                    UnitOfMeasure = article.UnitOfMeasure
                };

                _workContainer.SaleDetail.Add(saleDetail);

                article.Stock -= cartItem.Quantity;
                _workContainer.Article.Update(article);
            }

            _workContainer.Save();

            SaveCart(new List<ShoppingCartItemVM>());

            TempData["success"] = "Venta registrada correctamente";
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
            var item = cart.FirstOrDefault(i => i.ArticleId == article.Id);
            decimal quantityInCart = item?.Quantity ?? 0;

            if (quantityInCart + quantity > article.Stock)
            {
                message = "La cantidad supera el stock disponible";
                return false;
            }

            if (item == null)
            {
                cart.Add(new ShoppingCartItemVM
                {
                    ArticleId = article.Id,
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
                var article = _workContainer.Article.Get(cartItem.ArticleId);

                if (article == null || !article.isActive)
                {
                    continue;
                }

                items.Add(new CartItemVM
                {
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

            string normalizedSearch = searchString.Trim().ToLower();

            return _workContainer.Article.GetAll(
                a => a.isActive == true,
                includeProperties: "Category"
            )
            .Where(a =>
                a.Name.ToLower().Contains(normalizedSearch) ||
                a.Code.ToLower().Contains(normalizedSearch) ||
                (a.Category != null && a.Category.Name.ToLower().Contains(normalizedSearch))
            );
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

        private bool TryParseDecimal(string? value, out decimal result)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            value = value.Trim().Replace(" ", "");
            bool hasComma = value.Contains(",");
            bool hasDot = value.Contains(".");

            if (hasComma && hasDot)
            {
                int lastComma = value.LastIndexOf(",");
                int lastDot = value.LastIndexOf(".");

                value = lastComma > lastDot
                    ? value.Replace(".", "").Replace(",", ".")
                    : value.Replace(",", "");
            }
            else if (hasComma)
            {
                value = value.Replace(",", ".");
            }
            else if (hasDot)
            {
                int lastDot = value.LastIndexOf(".");
                int digitsAfterDot = value.Length - lastDot - 1;

                if (digitsAfterDot == 3)
                {
                    value = value.Replace(".", "");
                }
            }

            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
        }
    }
}
