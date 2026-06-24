using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rincon.DataAccess.Data;
using Rincon.DataAccess.Data.Repository.IRepository;
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
    public class CashRegisterController : Controller
    {
        private readonly IWorkContainer _workContainer;
        private readonly ApplicationDbContext _db;

        public CashRegisterController(IWorkContainer workContainer, ApplicationDbContext db)
        {
            _workContainer = workContainer;
            _db = db;
        }

        public IActionResult Index()
        {
            var userId = GetCurrentUserId();
            var canViewAllSessions = User.IsInRole(SD.Role_Admin);

            var openSession = GetOpenSession(userId);
            var sessions = _workContainer.CashRegisterSession
                .GetAll(
                    s => canViewAllSessions || s.UserId == userId,
                    orderBy: q => q.OrderByDescending(s => s.OpenedAt),
                    includeProperties: "User")
                .Take(20)
                .ToList();

            var vm = new CashRegisterVM
            {
                OpenSession = openSession,
                Sessions = sessions,
                CurrentSummary = openSession != null ? BuildSummary(openSession) : new CashRegisterSummaryVM(),
                CanViewAllSessions = canViewAllSessions
            };

            return View(vm);
        }

        public IActionResult Detail(int id)
        {
            var session = _db.CashRegisterSessions
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefault(s => s.Id == id);

            if (session == null)
            {
                return NotFound();
            }

            if (!CanAccessSession(session))
            {
                return Forbid();
            }

            ViewBag.Summary = BuildSummary(session);
            return View(session);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Open(CashRegisterOpenVM vm)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["error"] = "No se pudo identificar el usuario actual";
                return RedirectToAction(nameof(Index));
            }

            if (GetOpenSession(userId) != null)
            {
                TempData["error"] = "Ya tenes una caja abierta";
                return RedirectToAction(nameof(Index));
            }

            if (!TryParseDecimal(vm.OpeningAmountText, out decimal openingAmount) || openingAmount < 0)
            {
                TempData["error"] = "Ingrese un monto inicial valido";
                return RedirectToAction(nameof(Index));
            }

            _workContainer.CashRegisterSession.Add(new CashRegisterSession
            {
                OpenedAt = DateTime.Now,
                OpeningAmount = openingAmount,
                UserId = userId
            });

            _workContainer.Save();

            TempData["success"] = "Caja abierta correctamente";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Close(CashRegisterCloseVM vm)
        {
            var userId = GetCurrentUserId();
            var session = _workContainer.CashRegisterSession.GetFirstOrDefault(
                s => s.Id == vm.Id,
                includeProperties: "User");

            if (session == null || session.ClosedAt != null)
            {
                TempData["error"] = "La caja no esta disponible para cierre";
                return RedirectToAction(nameof(Index));
            }

            if (!User.IsInRole(SD.Role_Admin) && session.UserId != userId)
            {
                TempData["error"] = "No podes cerrar una caja de otro usuario";
                return RedirectToAction(nameof(Index));
            }

            if (!TryParseDecimal(vm.CountedCashAmountText, out decimal countedCash) || countedCash < 0)
            {
                TempData["error"] = "Ingrese el efectivo contado";
                return RedirectToAction(nameof(Index));
            }

            var summary = BuildSummary(session);

            session.ClosedAt = DateTime.Now;
            session.CountedCashAmount = countedCash;
            session.ExpectedCashAmount = summary.ExpectedCash;
            session.Difference = countedCash - summary.ExpectedCash;
            session.Notes = vm.Notes;

            _workContainer.CashRegisterSession.Update(session);
            _workContainer.Save();

            TempData["modalTitle"] = $"Caja #{session.Id} cerrada correctamente";
            TempData["modalText"] = $"Efectivo esperado: $ {session.ExpectedCashAmount.GetValueOrDefault().ToString("N2")}. Efectivo contado: $ {session.CountedCashAmount.GetValueOrDefault().ToString("N2")}. Diferencia: $ {session.Difference.GetValueOrDefault().ToString("N2")}.";
            TempData["modalIcon"] = "success";
            TempData["modalConfirmText"] = "Entendido";
            return RedirectToAction(nameof(Index));
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
            var userId = GetCurrentUserId();
            var canViewAllSessions = User.IsInRole(SD.Role_Admin);

            var query = _db.CashRegisterSessions
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => canViewAllSessions || s.UserId == userId)
                .AsQueryable();

            var recordsTotal = query.Count();

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                var isOpenSearch = "abierta".Contains(searchValue, StringComparison.OrdinalIgnoreCase);
                var isClosedSearch = "cerrada".Contains(searchValue, StringComparison.OrdinalIgnoreCase);

                query = query.Where(s =>
                    (s.User != null &&
                        ((s.User.FullName != null && s.User.FullName.Contains(searchValue)) ||
                         (s.User.Email != null && s.User.Email.Contains(searchValue)))) ||
                    s.Id.ToString().Contains(searchValue) ||
                    (s.Notes != null && s.Notes.Contains(searchValue)) ||
                    (isOpenSearch && s.ClosedAt == null) ||
                    (isClosedSearch && s.ClosedAt != null));
            }

            var recordsFiltered = query.Count();

            query = orderColumn switch
            {
                0 => orderDirection == "asc" ? query.OrderBy(s => s.Id) : query.OrderByDescending(s => s.Id),
                1 => orderDirection == "asc"
                    ? query.OrderBy(s => s.User != null ? s.User.FullName : string.Empty)
                    : query.OrderByDescending(s => s.User != null ? s.User.FullName : string.Empty),
                2 => orderDirection == "asc" ? query.OrderBy(s => s.OpenedAt) : query.OrderByDescending(s => s.OpenedAt),
                3 => orderDirection == "asc" ? query.OrderBy(s => s.ClosedAt) : query.OrderByDescending(s => s.ClosedAt),
                4 => orderDirection == "asc" ? query.OrderBy(s => s.OpeningAmount) : query.OrderByDescending(s => s.OpeningAmount),
                5 => orderDirection == "asc" ? query.OrderBy(s => s.ExpectedCashAmount) : query.OrderByDescending(s => s.ExpectedCashAmount),
                6 => orderDirection == "asc" ? query.OrderBy(s => s.CountedCashAmount) : query.OrderByDescending(s => s.CountedCashAmount),
                7 => orderDirection == "asc" ? query.OrderBy(s => s.Difference) : query.OrderByDescending(s => s.Difference),
                8 => orderDirection == "asc" ? query.OrderBy(s => s.ClosedAt == null) : query.OrderByDescending(s => s.ClosedAt == null),
                _ => query.OrderByDescending(s => s.OpenedAt)
            };

            var data = query
                .Skip(start)
                .Take(length)
                .ToList()
                .Select(session => new
            {
                id = session.Id,
                cashRegisterNumber = $"#{session.Id}",
                user = GetUserName(session),
                openedAt = session.OpenedAt.ToString("dd/MM/yyyy HH:mm"),
                openedAtSort = session.OpenedAt.ToString("yyyyMMddHHmmss"),
                closedAt = session.ClosedAt.HasValue ? session.ClosedAt.Value.ToString("dd/MM/yyyy HH:mm") : "-",
                closedAtSort = session.ClosedAt.HasValue ? session.ClosedAt.Value.ToString("yyyyMMddHHmmss") : string.Empty,
                openingAmount = session.OpeningAmount.ToString("N2"),
                expectedCashAmount = session.ExpectedCashAmount.HasValue ? session.ExpectedCashAmount.Value.ToString("N2") : "-",
                countedCashAmount = session.CountedCashAmount.HasValue ? session.CountedCashAmount.Value.ToString("N2") : "-",
                difference = session.Difference.HasValue ? session.Difference.Value.ToString("N2") : "-",
                differenceValue = session.Difference ?? 0,
                status = session.IsOpen ? "Abierta" : "Cerrada",
                isOpen = session.IsOpen,
                detailUrl = Url.Action("Detail", "CashRegister", new { area = "Employee", id = session.Id })
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
        public IActionResult GetDetailItems(int id)
        {
            var session = _db.CashRegisterSessions
                .AsNoTracking()
                .FirstOrDefault(s => s.Id == id);

            if (session == null)
            {
                return NotFound();
            }

            if (!CanAccessSession(session))
            {
                return Forbid();
            }

            var draw = GetDataTablesInt("draw");
            var start = GetDataTablesInt("start");
            var length = GetDataTablesInt("length", 5);
            var searchValue = Request.Query["search[value]"].ToString()?.Trim();
            var orderColumn = GetDataTablesInt("order[0][column]");
            var orderDirection = Request.Query["order[0][dir]"].ToString();

            var saleRows = _db.SaleDetails
                .AsNoTracking()
                .Include(d => d.Sale)
                    .ThenInclude(s => s.PersonalAccount)
                .Where(d => d.Sale.CashRegisterSessionId == id && !d.Sale.IsVoided)
                .ToList()
                .Select(d => new CashRegisterDetailRow
                {
                    SaleId = d.SaleId,
                    Date = d.Sale.Date,
                    PaymentMethod = d.Sale.PaymentMethod.ToString(),
                    ArticleName = d.ArticleName,
                    ArticleCode = d.ArticleCode,
                    Quantity = FormatQuantity(d.Quantity, d.UnitOfMeasure),
                    UnitPrice = d.UnitPrice,
                    Subtotal = d.Subtotal,
                    PersonalAccount = d.Sale.PersonalAccount != null ? d.Sale.PersonalAccount.FullName : "-",
                    PersonalAccountUrl = d.Sale.PersonalAccountId.HasValue
                        ? Url.Action("Detail", "PersonalAccounts", new { area = "Employee", id = d.Sale.PersonalAccountId.Value })
                        : null,
                    DebtStatus = d.Sale.PaymentMethod == PaymentMethod.CuentaPersonal
                        ? d.Sale.IsPersonalAccountSettled ? "Saldada" : "Pendiente"
                        : "-",
                    DebtSettled = d.Sale.IsPersonalAccountSettled,
                    MovementStatus = "Venta",
                    MovementStatusClass = "status-active"
                });

            var voidRows = _db.SaleReturnDetails
                .AsNoTracking()
                .Include(d => d.SaleReturn)
                .Include(d => d.SaleDetail)
                    .ThenInclude(sd => sd.Sale)
                        .ThenInclude(s => s.PersonalAccount)
                .Where(d => d.SaleReturn != null && d.SaleReturn.CashRegisterSessionId == id)
                .ToList()
                .Select(d => new CashRegisterDetailRow
                {
                    SaleId = d.SaleDetail?.SaleId ?? 0,
                    Date = d.SaleReturn?.Date ?? DateTime.MinValue,
                    PaymentMethod = d.SaleReturn?.PaymentMethod.ToString() ?? "-",
                    ArticleName = d.SaleDetail?.ArticleName ?? "Producto no disponible",
                    ArticleCode = d.SaleDetail?.ArticleCode,
                    Quantity = FormatQuantity(d.Quantity, d.UnitOfMeasure),
                    UnitPrice = d.UnitPrice,
                    Subtotal = -d.Subtotal,
                    PersonalAccount = d.SaleDetail?.Sale?.PersonalAccount != null ? d.SaleDetail.Sale.PersonalAccount.FullName : "-",
                    PersonalAccountUrl = d.SaleDetail?.Sale?.PersonalAccountId.HasValue == true
                        ? Url.Action("Detail", "PersonalAccounts", new { area = "Employee", id = d.SaleDetail.Sale.PersonalAccountId.Value })
                        : null,
                    DebtStatus = "Anulada",
                    DebtSettled = true,
                    MovementStatus = "Anulación",
                    MovementStatusClass = "status-inactive"
                });

            var exchangeRows = _db.SaleExchanges
                .AsNoTracking()
                .Include(e => e.Sale)
                    .ThenInclude(s => s.PersonalAccount)
                .Include(e => e.ReplacementArticle)
                .Where(e => e.CashRegisterSessionId == id)
                .ToList()
                .Select(e => new CashRegisterDetailRow
                {
                    SaleId = e.SaleId,
                    Date = e.Date,
                    PaymentMethod = "Sin movimiento de caja",
                    ArticleName = e.ReplacementArticle != null ? e.ReplacementArticle.Name : "Producto de reemplazo",
                    ArticleCode = e.ReplacementArticle?.Code,
                    Quantity = FormatQuantity(e.Quantity, e.UnitOfMeasure),
                    UnitPrice = 0,
                    Subtotal = e.EstimatedLoss,
                    PersonalAccount = e.Sale?.PersonalAccount != null ? e.Sale.PersonalAccount.FullName : "-",
                    PersonalAccountUrl = e.Sale?.PersonalAccountId.HasValue == true
                        ? Url.Action("Detail", "PersonalAccounts", new { area = "Employee", id = e.Sale.PersonalAccountId.Value })
                        : null,
                    DebtStatus = "-",
                    DebtSettled = true,
                    MovementStatus = "Recambio",
                    MovementStatusClass = "status-warning"
                });

            var rows = saleRows.Concat(voidRows).Concat(exchangeRows).ToList();

            var recordsTotal = rows.Count;

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                var settledSearch = "saldada".Contains(searchValue, StringComparison.OrdinalIgnoreCase);
                var pendingSearch = "pendiente".Contains(searchValue, StringComparison.OrdinalIgnoreCase);
                var voidSearch = "anulación".Contains(searchValue, StringComparison.OrdinalIgnoreCase) || "anulada".Contains(searchValue, StringComparison.OrdinalIgnoreCase);
                var exchangeSearch = "recambio".Contains(searchValue, StringComparison.OrdinalIgnoreCase);

                rows = rows.Where(d =>
                    d.SaleId.ToString().Contains(searchValue) ||
                    d.ArticleName.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                    (d.ArticleCode != null && d.ArticleCode.Contains(searchValue, StringComparison.OrdinalIgnoreCase)) ||
                    d.PaymentMethod.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                    d.PersonalAccount.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                    d.MovementStatus.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                    (settledSearch && d.DebtStatus == "Saldada") ||
                    (pendingSearch && d.DebtStatus == "Pendiente") ||
                    (voidSearch && d.MovementStatus == "Anulación") ||
                    (exchangeSearch && d.MovementStatus == "Recambio")).ToList();
            }

            var recordsFiltered = rows.Count;

            rows = (orderColumn switch
            {
                0 => orderDirection == "asc" ? rows.OrderBy(d => d.Date) : rows.OrderByDescending(d => d.Date),
                1 => orderDirection == "asc" ? rows.OrderBy(d => d.SaleId) : rows.OrderByDescending(d => d.SaleId),
                2 => orderDirection == "asc" ? rows.OrderBy(d => d.MovementStatus) : rows.OrderByDescending(d => d.MovementStatus),
                3 => orderDirection == "asc" ? rows.OrderBy(d => d.PaymentMethod) : rows.OrderByDescending(d => d.PaymentMethod),
                4 => orderDirection == "asc" ? rows.OrderBy(d => d.ArticleName) : rows.OrderByDescending(d => d.ArticleName),
                5 => orderDirection == "asc" ? rows.OrderBy(d => d.Quantity) : rows.OrderByDescending(d => d.Quantity),
                6 => orderDirection == "asc" ? rows.OrderBy(d => d.UnitPrice) : rows.OrderByDescending(d => d.UnitPrice),
                7 => orderDirection == "asc" ? rows.OrderBy(d => d.Subtotal) : rows.OrderByDescending(d => d.Subtotal),
                8 => orderDirection == "asc" ? rows.OrderBy(d => d.PersonalAccount) : rows.OrderByDescending(d => d.PersonalAccount),
                _ => rows.OrderByDescending(d => d.Date)
            }).ToList();

            var data = rows
                .Skip(start)
                .Take(length)
                .Select(d => new
                {
                    saleId = d.SaleId,
                    saleNumber = $"#{d.SaleId}",
                    date = d.Date.ToString("dd/MM/yyyy HH:mm"),
                    dateSort = d.Date.ToString("yyyyMMddHHmmss"),
                    paymentMethod = d.PaymentMethod,
                    movementStatus = d.MovementStatus,
                    movementStatusClass = d.MovementStatusClass,
                    articleName = d.ArticleName,
                    articleCode = d.ArticleCode,
                    quantity = d.Quantity,
                    unitPrice = d.UnitPrice.ToString("N2"),
                    subtotal = d.Subtotal.ToString("N2"),
                    subtotalValue = d.Subtotal,
                    personalAccount = d.PersonalAccount,
                    personalAccountUrl = d.PersonalAccountUrl,
                    debtStatus = d.DebtStatus,
                    debtSettled = d.DebtSettled
                });

            return Json(new
            {
                draw,
                recordsTotal,
                recordsFiltered,
                data
            });
        }

        private CashRegisterSession? GetOpenSession(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return _workContainer.CashRegisterSession.GetFirstOrDefault(
                s => s.UserId == userId && s.ClosedAt == null,
                includeProperties: "User");
        }

        private CashRegisterSummaryVM BuildSummary(CashRegisterSession session)
        {
            var sales = _workContainer.Sale
                .GetAll(s => s.CashRegisterSessionId == session.Id && !s.IsVoided)
                .ToList();

            var returns = _workContainer.SaleReturn
                .GetAll(r => r.CashRegisterSessionId == session.Id)
                .ToList();

            var accountPayments = _workContainer.PersonalAccountPayment
                .GetAll(p => p.CashRegisterSessionId == session.Id)
                .ToList();

            var exchanges = _db.SaleExchanges
                .AsNoTracking()
                .Include(e => e.Sale)
                .Where(e => e.CashRegisterSessionId == session.Id)
                .ToList();

            var cashSales = sales
                .Where(s => s.PaymentMethod == PaymentMethod.Efectivo)
                .Sum(s => s.Total);

            var transferSales = sales
                .Where(s => s.PaymentMethod == PaymentMethod.Transferencia)
                .Sum(s => s.Total);

            var personalAccountSales = sales
                .Where(s => s.PaymentMethod == PaymentMethod.CuentaPersonal)
                .Sum(s => s.Total);

            var personalAccountCashPayments = accountPayments
                .Where(p => p.PaymentMethod == PaymentMethod.Efectivo)
                .Sum(p => p.Amount);

            var personalAccountTransferPayments = accountPayments
                .Where(p => p.PaymentMethod == PaymentMethod.Transferencia)
                .Sum(p => p.Amount);

            var cashReturns = returns
                .Where(r => r.PaymentMethod == PaymentMethod.Efectivo)
                .Sum(r => r.Total);

            var transferReturns = returns
                .Where(r => r.PaymentMethod == PaymentMethod.Transferencia)
                .Sum(r => r.Total);

            var personalAccountReturns = returns
                .Where(r => r.PaymentMethod == PaymentMethod.CuentaPersonal)
                .Sum(r => r.Total);

            var cashExchangeLoss = exchanges
                .Where(e => e.Sale != null && e.Sale.PaymentMethod == PaymentMethod.Efectivo)
                .Sum(e => e.EstimatedLoss);

            var transferExchangeLoss = exchanges
                .Where(e => e.Sale != null && e.Sale.PaymentMethod == PaymentMethod.Transferencia)
                .Sum(e => e.EstimatedLoss);

            var personalAccountExchangeLoss = exchanges
                .Where(e => e.Sale != null && e.Sale.PaymentMethod == PaymentMethod.CuentaPersonal)
                .Sum(e => e.EstimatedLoss);

            var totalSales = sales.Sum(s => s.Total);
            var totalReturns = returns.Sum(r => r.Total);

            return new CashRegisterSummaryVM
            {
                CashSales = cashSales,
                TransferSales = transferSales,
                PersonalAccountSales = personalAccountSales,
                PersonalAccountCashPayments = personalAccountCashPayments,
                PersonalAccountTransferPayments = personalAccountTransferPayments,
                TotalSales = totalSales,
                CashReturns = cashReturns,
                TransferReturns = transferReturns,
                PersonalAccountReturns = personalAccountReturns,
                TotalReturns = totalReturns,
                NetTotal = totalSales - totalReturns,
                ExpectedCash = session.OpeningAmount + cashSales + personalAccountCashPayments - cashReturns,
                ExchangeLoss = exchanges.Sum(e => e.EstimatedLoss),
                CashExchangeLoss = cashExchangeLoss,
                TransferExchangeLoss = transferExchangeLoss,
                PersonalAccountExchangeLoss = personalAccountExchangeLoss,
                SalesCount = sales.Count,
                ReturnsCount = returns.Count,
                ExchangesCount = exchanges.Count
            };
        }

        private string? GetCurrentUserId()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            return claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private bool CanAccessSession(CashRegisterSession session)
        {
            return User.IsInRole(SD.Role_Admin) || session.UserId == GetCurrentUserId();
        }

        private string GetUserName(CashRegisterSession session)
        {
            return session.User != null
                ? (!string.IsNullOrWhiteSpace(session.User.FullName) ? session.User.FullName : session.User.Email ?? "Sin usuario")
                : "Sin usuario";
        }

        private int GetDataTablesInt(string key, int defaultValue = 0)
        {
            return int.TryParse(Request.Query[key], out var value) ? value : defaultValue;
        }

        private string FormatQuantity(decimal quantity, string unitOfMeasure)
        {
            var formatted = quantity % 1 == 0
                ? quantity.ToString("N0")
                : quantity.ToString("N3").TrimEnd('0').TrimEnd(',');

            return unitOfMeasure == "Kilogramo" ? $"{formatted} kg" : $"{formatted} u";
        }

        private bool TryParseDecimal(string? value, out decimal result) => DecimalParser.TryParse(value, out result);

        private sealed class CashRegisterDetailRow
        {
            public int SaleId { get; set; }
            public DateTime Date { get; set; }
            public string PaymentMethod { get; set; } = string.Empty;
            public string MovementStatus { get; set; } = string.Empty;
            public string MovementStatusClass { get; set; } = string.Empty;
            public string ArticleName { get; set; } = string.Empty;
            public string? ArticleCode { get; set; }
            public string Quantity { get; set; } = string.Empty;
            public decimal UnitPrice { get; set; }
            public decimal Subtotal { get; set; }
            public string PersonalAccount { get; set; } = string.Empty;
            public string? PersonalAccountUrl { get; set; }
            public string DebtStatus { get; set; } = string.Empty;
            public bool DebtSettled { get; set; }
        }
    }
}
