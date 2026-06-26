using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rincon.DataAccess.Data;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;
using Rincon.Models.ViewModels;
using Rincon.Utilities;
using Rincon.Utilities.Enums;
using System.Security.Claims;

namespace Rincon.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class SalesController : Controller
    {
        private readonly IWorkContainer _workContainer;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public SalesController(IWorkContainer workContainer, UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _workContainer = workContainer;
            _userManager = userManager;
            _db = db;
        }

        public IActionResult Index(string? userId, DateTime? saleDate)
        {
            var vm = new SalesIndexVM
            {
                UserId = userId,
                SaleDate = saleDate,
                UserList = _userManager.Users
                    .OrderBy(u => u.FullName)
                    .Select(u => new SelectListItem
                    {
                        Text = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Email,
                        Value = u.Id
                    })
                    .ToList()
            };

            return View(vm);
        }

        public IActionResult Voided(string? userId, DateTime? saleDate)
        {
            var vm = new SalesIndexVM
            {
                UserId = userId,
                SaleDate = saleDate,
                UserList = _userManager.Users
                    .OrderBy(u => u.FullName)
                    .Select(u => new SelectListItem
                    {
                        Text = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Email,
                        Value = u.Id
                    })
                    .ToList()
            };

            return View(vm);
        }

        public IActionResult Detail(int id)
        {
            var sale = _db.Sales
                .AsNoTracking()
                .Include(s => s.User)
                .Include(s => s.CashRegisterSession)
                    .ThenInclude(c => c.User)
                .Include(s => s.PersonalAccount)
                .Include(s => s.SaleReturns)
                    .ThenInclude(r => r.SaleReturnDetails)
                .Include(s => s.SaleExchanges)
                    .ThenInclude(e => e.ReplacementArticle)
                .Include(s => s.SaleDetails)
                    .ThenInclude(d => d.SaleDetailBatches)
                .FirstOrDefault(s => s.Id == id);

            if (sale == null)
            {
                return NotFound();
            }

            return View(sale);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Void(int id, string? reason)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["error"] = "No se pudo identificar el usuario actual";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var openCashRegister = _workContainer.CashRegisterSession.GetFirstOrDefault(
                s => s.UserId == userId && s.ClosedAt == null);

            if (openCashRegister == null)
            {
                TempData["error"] = "Debe abrir una caja antes de anular ventas o registrar devoluciones";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var sale = _db.Sales
                .Include(s => s.SaleReturns)
                .Include(s => s.SaleDetails)
                    .ThenInclude(d => d.SaleDetailBatches)
                .FirstOrDefault(s => s.Id == id);

            if (sale == null)
            {
                return NotFound();
            }

            if (sale.IsVoided)
            {
                TempData["error"] = "La venta ya está anulada";
                return RedirectToAction(nameof(Detail), new { id });
            }

            if (sale.SaleReturns.Any())
            {
                TempData["error"] = "La venta ya tiene devoluciones registradas. No se puede anular completa.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            using var transaction = _db.Database.BeginTransaction();

            try
            {
                var saleReturn = new SaleReturn
                {
                    SaleId = sale.Id,
                    Date = DateTime.Now,
                    UserId = userId,
                    CashRegisterSessionId = openCashRegister.Id,
                    PaymentMethod = sale.PaymentMethod,
                    Total = sale.Total,
                    IsFullVoid = true,
                    Reason = string.IsNullOrWhiteSpace(reason) ? "Anulación completa" : reason.Trim()
                };

                _workContainer.SaleReturn.Add(saleReturn);
                _workContainer.Save();

                foreach (var detail in sale.SaleDetails)
                {
                    var saleReturnDetail = new SaleReturnDetail
                    {
                        SaleReturnId = saleReturn.Id,
                        SaleDetailId = detail.Id,
                        ArticleId = detail.ArticleId,
                        Quantity = detail.Quantity,
                        UnitPrice = detail.UnitPrice,
                        Subtotal = detail.Subtotal,
                        UnitOfMeasure = detail.UnitOfMeasure
                    };

                    _workContainer.SaleReturnDetail.Add(saleReturnDetail);
                    _workContainer.Save();

                    RestoreStockFromVoidedSaleDetail(detail, saleReturnDetail.Id);
                }

                sale.IsVoided = true;
                sale.VoidedAt = DateTime.Now;
                sale.VoidedByUserId = userId;
                sale.VoidReason = string.IsNullOrWhiteSpace(reason) ? "Anulación completa" : reason.Trim();

                if (sale.PaymentMethod == PaymentMethod.CuentaPersonal)
                {
                    sale.PersonalAccountPaidAmount = sale.Total;
                    sale.IsPersonalAccountSettled = true;
                    sale.PersonalAccountSettledAt = DateTime.Now;
                }

                _workContainer.Sale.Update(sale);
                _workContainer.Save();
                transaction.Commit();

                TempData["modalTitle"] = $"Venta #{sale.Id} anulada";
                TempData["modalText"] = $"Se registró la anulación por $ {sale.Total:N2}. El stock fue repuesto y el movimiento impactó en la caja #{openCashRegister.Id}.";
                TempData["modalIcon"] = "success";
                TempData["modalConfirmText"] = "Entendido";
            }
            catch
            {
                transaction.Rollback();
                TempData["error"] = "No se pudo anular la venta. No se modificó stock ni caja.";
            }

            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Exchange(int id, int saleDetailId, string? quantityText, string? reason)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["error"] = "No se pudo identificar el usuario actual";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var openCashRegister = _workContainer.CashRegisterSession.GetFirstOrDefault(
                s => s.UserId == userId && s.ClosedAt == null);

            if (openCashRegister == null)
            {
                TempData["error"] = "Debe abrir una caja antes de registrar recambios";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var sale = _db.Sales
                .Include(s => s.SaleDetails)
                .FirstOrDefault(s => s.Id == id);

            if (sale == null)
            {
                return NotFound();
            }

            if (sale.IsVoided)
            {
                TempData["error"] = "No se pueden registrar recambios sobre una venta anulada";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var saleDetail = sale.SaleDetails.FirstOrDefault(d => d.Id == saleDetailId);

            if (saleDetail == null)
            {
                TempData["error"] = "No se encontró el producto vendido para recambio";
                return RedirectToAction(nameof(Detail), new { id });
            }

            if (!saleDetail.ArticleId.HasValue)
            {
                TempData["error"] = "No se puede registrar recambio de un producto manual porque no tiene stock asociado";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var replacementArticle = _workContainer.Article.Get(saleDetail.ArticleId.Value);

            if (replacementArticle == null || !replacementArticle.isActive)
            {
                TempData["error"] = "El producto vendido ya no está disponible para registrar el recambio";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var quantityToExchange = string.IsNullOrWhiteSpace(quantityText)
                ? saleDetail.Quantity
                : DecimalParser.TryParse(quantityText, out var parsedQuantity) ? parsedQuantity : 0;

            if (quantityToExchange <= 0)
            {
                TempData["error"] = "Ingrese una cantidad válida para el recambio";
                return RedirectToAction(nameof(Detail), new { id });
            }

            if (!IsValidQuantityForArticle(replacementArticle, quantityToExchange))
            {
                TempData["error"] = $"El artículo {replacementArticle.Name} se vende por unidad. La cantidad del recambio debe ser entera";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var availableStock = GetAvailableStock(replacementArticle);

            if (quantityToExchange > availableStock)
            {
                TempData["error"] = $"Stock insuficiente para {replacementArticle.Name}. Stock disponible: {FormatQuantity(availableStock, replacementArticle.UnitOfMeasure)}";
                return RedirectToAction(nameof(Detail), new { id });
            }

            using var transaction = _db.Database.BeginTransaction();

            try
            {
                var unitCost = DeductArticleStock(replacementArticle, quantityToExchange, out var batchConsumptions);
                var exchange = new SaleExchange
                {
                    SaleId = sale.Id,
                    SaleDetailId = saleDetail.Id,
                    OriginalArticleId = saleDetail.ArticleId,
                    ReplacementArticleId = replacementArticle.Id,
                    Quantity = quantityToExchange,
                    ReplacementUnitCost = unitCost,
                    EstimatedLoss = unitCost * quantityToExchange,
                    UnitOfMeasure = replacementArticle.UnitOfMeasure,
                    Date = DateTime.Now,
                    UserId = userId,
                    CashRegisterSessionId = openCashRegister.Id,
                    Reason = string.IsNullOrWhiteSpace(reason) ? "Recambio por falla" : reason.Trim()
                };

                _workContainer.SaleExchange.Add(exchange);
                _workContainer.Save();

                foreach (var batchConsumption in batchConsumptions)
                {
                    _workContainer.SaleExchangeBatch.Add(new SaleExchangeBatch
                    {
                        SaleExchangeId = exchange.Id,
                        ArticleBatchId = batchConsumption.ArticleBatchId,
                        Quantity = batchConsumption.Quantity,
                        UnitCost = batchConsumption.UnitCost
                    });
                }

                _workContainer.Save();
                transaction.Commit();

                TempData["modalTitle"] = "Recambio registrado";
                TempData["modalText"] = $"Se descontó {FormatQuantity(quantityToExchange, replacementArticle.UnitOfMeasure)} de {replacementArticle.Name} como reposición del mismo producto. No se modificó la caja porque el cliente ya había pagado la venta original.";
                TempData["modalIcon"] = "success";
                TempData["modalConfirmText"] = "Entendido";
            }
            catch
            {
                transaction.Rollback();
                TempData["error"] = "No se pudo registrar el recambio. No se modificó stock.";
            }

            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpGet]
        public IActionResult GetAll(string? userId, DateTime? saleDate)
        {
            var draw = GetDataTablesInt("draw");
            var start = GetDataTablesInt("start");
            var length = GetDataTablesInt("length", 5);
            var searchValue = Request.Query["search[value]"].ToString()?.Trim();
            var orderColumn = GetDataTablesInt("order[0][column]");
            var orderDirection = Request.Query["order[0][dir]"].ToString();

            var query = _db.Sales
                .AsNoTracking()
                .Include(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(s => s.UserId == userId);
            }

            if (saleDate.HasValue)
            {
                var from = saleDate.Value.Date;
                var to = from.AddDays(1);
                query = query.Where(s => s.Date >= from && s.Date < to);
            }

            var recordsTotal = query.Count();

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                var searchPattern = $"%{searchValue}%";
                var matchingPaymentMethods = Enum.GetValues<PaymentMethod>()
                    .Where(p => p.ToString().Contains(searchValue, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var hasTotalSearch = DecimalParser.TryParse(searchValue, out var totalSearch);

                query = query.Where(s =>
                    (hasTotalSearch && s.Total == totalSearch) ||
                    matchingPaymentMethods.Contains(s.PaymentMethod) ||
                    (s.User != null &&
                        ((s.User.FullName != null && EF.Functions.ILike(s.User.FullName, searchPattern)) ||
                         (s.User.Email != null && EF.Functions.ILike(s.User.Email, searchPattern)))));
            }

            var recordsFiltered = query.Count();

            query = orderColumn switch
            {
                0 => orderDirection == "asc" ? query.OrderBy(s => s.Date) : query.OrderByDescending(s => s.Date),
                1 => orderDirection == "asc" ? query.OrderBy(s => s.PaymentMethod) : query.OrderByDescending(s => s.PaymentMethod),
                2 => orderDirection == "asc" ? query.OrderBy(s => s.Total) : query.OrderByDescending(s => s.Total),
                3 => orderDirection == "asc"
                    ? query.OrderBy(s => s.User != null ? s.User.FullName : string.Empty)
                    : query.OrderByDescending(s => s.User != null ? s.User.FullName : string.Empty),
                4 => orderDirection == "asc" ? query.OrderBy(s => s.IsVoided) : query.OrderByDescending(s => s.IsVoided),
                _ => query.OrderByDescending(s => s.Date)
            };

            var salesData = query
                .Skip(start)
                .Take(length)
                .ToList()
                .Select(s => new
                {
                    id = s.Id,
                    date = s.Date.ToString("dd/MM/yyyy HH:mm"),
                    total = s.Total.ToString("N2"),
                    paymentMethod = s.PaymentMethod.ToString(),
                    user = s.User != null
                        ? (!string.IsNullOrWhiteSpace(s.User.FullName) ? s.User.FullName : s.User.Email)
                        : "Sin usuario",
                    isVoided = s.IsVoided,
                    status = s.IsVoided ? "Anulada" : "Activa",
                    detailUrl = Url.Action("Detail", "Sales", new { area = "Admin", id = s.Id })
                });

            return Json(new
            {
                draw,
                recordsTotal,
                recordsFiltered,
                data = salesData
            });
        }

        [HttpGet]
        public IActionResult GetVoided(string? userId, DateTime? saleDate)
        {
            var draw = GetDataTablesInt("draw");
            var start = GetDataTablesInt("start");
            var length = GetDataTablesInt("length", 5);
            var searchValue = Request.Query["search[value]"].ToString()?.Trim();
            var orderColumn = GetDataTablesInt("order[0][column]");
            var orderDirection = Request.Query["order[0][dir]"].ToString();

            var voidedRows = _db.Sales
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => s.IsVoided)
                .Select(s => new MovementRow
                {
                    SaleId = s.Id,
                    SaleDate = s.Date,
                    MovementDate = s.VoidedAt ?? s.Date,
                    PaymentMethod = s.PaymentMethod.ToString(),
                    Total = s.Total,
                    UserId = s.UserId,
                    User = s.User != null
                        ? (!string.IsNullOrWhiteSpace(s.User.FullName) ? s.User.FullName : s.User.Email ?? "Sin usuario")
                        : "Sin usuario",
                    Status = "Anulada",
                    StatusClass = "status-inactive",
                    DetailUrl = Url.Action("Detail", "Sales", new { area = "Admin", id = s.Id }) ?? string.Empty
                })
                .ToList();

            var exchangeRows = _db.SaleExchanges
                .AsNoTracking()
                .Include(e => e.User)
                .Include(e => e.Sale)
                .Include(e => e.ReplacementArticle)
                .Select(e => new MovementRow
                {
                    SaleId = e.SaleId,
                    SaleDate = e.Sale != null ? e.Sale.Date : e.Date,
                    MovementDate = e.Date,
                    PaymentMethod = "Sin movimiento de caja",
                    Total = e.EstimatedLoss,
                    UserId = e.UserId,
                    User = e.User != null
                        ? (!string.IsNullOrWhiteSpace(e.User.FullName) ? e.User.FullName : e.User.Email ?? "Sin usuario")
                        : "Sin usuario",
                    Status = "Recambio",
                    StatusClass = "status-warning",
                    Product = e.ReplacementArticle != null ? e.ReplacementArticle.Name : string.Empty,
                    DetailUrl = Url.Action("Detail", "Sales", new { area = "Admin", id = e.SaleId }) ?? string.Empty
                })
                .ToList();

            var rows = voidedRows.Concat(exchangeRows).ToList();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                rows = rows.Where(r => r.UserId == userId).ToList();
            }

            if (saleDate.HasValue)
            {
                var from = saleDate.Value.Date;
                var to = from.AddDays(1);
                rows = rows.Where(r => r.MovementDate >= from && r.MovementDate < to).ToList();
            }

            var recordsTotal = rows.Count;

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                var normalizedSearch = searchValue.ToLower();
                var hasTotalSearch = DecimalParser.TryParse(searchValue, out var totalSearch);

                rows = rows.Where(r =>
                    r.SaleId.ToString().Contains(searchValue) ||
                    (hasTotalSearch && r.Total == totalSearch) ||
                    r.Total.ToString("N2").Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                    r.PaymentMethod.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                    r.User.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                    r.Status.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                    r.Product.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                    "anulada".Contains(normalizedSearch) ||
                    "recambio".Contains(normalizedSearch)).ToList();
            }

            var recordsFiltered = rows.Count;

            rows = (orderColumn switch
            {
                0 => orderDirection == "asc" ? rows.OrderBy(r => r.SaleDate) : rows.OrderByDescending(r => r.SaleDate),
                1 => orderDirection == "asc" ? rows.OrderBy(r => r.MovementDate) : rows.OrderByDescending(r => r.MovementDate),
                2 => orderDirection == "asc" ? rows.OrderBy(r => r.PaymentMethod) : rows.OrderByDescending(r => r.PaymentMethod),
                3 => orderDirection == "asc" ? rows.OrderBy(r => r.Total) : rows.OrderByDescending(r => r.Total),
                4 => orderDirection == "asc" ? rows.OrderBy(r => r.User) : rows.OrderByDescending(r => r.User),
                5 => orderDirection == "asc" ? rows.OrderBy(r => r.Status) : rows.OrderByDescending(r => r.Status),
                _ => rows.OrderByDescending(r => r.MovementDate)
            }).ToList();

            var salesData = rows
                .Skip(start)
                .Take(length)
                .Select(r => new
                {
                    id = r.SaleId,
                    date = r.SaleDate.ToString("dd/MM/yyyy HH:mm"),
                    movementDate = r.MovementDate.ToString("dd/MM/yyyy HH:mm"),
                    total = r.Total.ToString("N2"),
                    paymentMethod = r.PaymentMethod,
                    user = r.User,
                    status = r.Status,
                    statusClass = r.StatusClass,
                    detailUrl = r.DetailUrl
                });

            return Json(new
            {
                draw,
                recordsTotal,
                recordsFiltered,
                data = salesData
            });
        }

        private int GetDataTablesInt(string key, int defaultValue = 0)
        {
            return int.TryParse(Request.Query[key], out var value) ? value : defaultValue;
        }

        private string? GetCurrentUserId()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            return claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private decimal GetAvailableStock(Article article)
        {
            if (!article.UsesBatches)
            {
                return article.Stock;
            }

            return _workContainer.ArticleBatch
                .GetAll(b => b.ArticleId == article.Id && b.IsActive && b.Quantity > 0)
                .Sum(b => b.Quantity);
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

        private void RestoreStockFromVoidedSaleDetail(SaleDetail detail, int saleReturnDetailId)
        {
            if (!detail.ArticleId.HasValue)
            {
                return;
            }

            if (detail.SaleDetailBatches.Any())
            {
                foreach (var consumedBatch in detail.SaleDetailBatches)
                {
                    var batch = _workContainer.ArticleBatch.Get(consumedBatch.ArticleBatchId);

                    if (batch == null)
                    {
                        continue;
                    }

                    batch.Quantity += consumedBatch.Quantity;
                    batch.IsActive = true;
                    _workContainer.ArticleBatch.Update(batch);

                    _workContainer.SaleReturnDetailBatch.Add(new SaleReturnDetailBatch
                    {
                        SaleReturnDetailId = saleReturnDetailId,
                        ArticleBatchId = batch.Id,
                        Quantity = consumedBatch.Quantity
                    });
                }

                var batchedArticle = _workContainer.Article.Get(detail.ArticleId.Value);

                if (batchedArticle != null)
                {
                    batchedArticle.Stock = _workContainer.ArticleBatch
                        .GetAll(b => b.ArticleId == batchedArticle.Id && b.IsActive)
                        .Sum(b => b.Quantity);
                    _workContainer.Article.Update(batchedArticle);
                }

                _workContainer.Save();
                return;
            }

            var article = _workContainer.Article.Get(detail.ArticleId.Value);

            if (article == null)
            {
                return;
            }

            article.Stock += detail.Quantity;
            _workContainer.Article.Update(article);
            _workContainer.Save();
        }

        private static bool IsValidQuantityForArticle(Article article, decimal quantity)
        {
            if (!article.IsSoldByWeight || article.UnitOfMeasure == "Unidad")
            {
                return decimal.Truncate(quantity) == quantity;
            }

            return true;
        }

        private static string FormatQuantity(decimal quantity, string unitOfMeasure)
        {
            var formatted = quantity % 1 == 0
                ? quantity.ToString("N0")
                : quantity.ToString("N3").TrimEnd('0').TrimEnd(',');

            return unitOfMeasure == "Kilogramo" ? $"{formatted} kg" : $"{formatted} u";
        }

        private sealed record BatchConsumption(int ArticleBatchId, decimal Quantity, decimal UnitCost);

        private sealed class MovementRow
        {
            public int SaleId { get; set; }
            public DateTime SaleDate { get; set; }
            public DateTime MovementDate { get; set; }
            public string PaymentMethod { get; set; } = string.Empty;
            public decimal Total { get; set; }
            public string? UserId { get; set; }
            public string User { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string StatusClass { get; set; } = string.Empty;
            public string Product { get; set; } = string.Empty;
            public string DetailUrl { get; set; } = string.Empty;
        }

    }
}
