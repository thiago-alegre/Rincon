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

            TempData["success"] = "Caja cerrada correctamente";
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
                    (s.Notes != null && s.Notes.Contains(searchValue)) ||
                    (isOpenSearch && s.ClosedAt == null) ||
                    (isClosedSearch && s.ClosedAt != null));
            }

            var recordsFiltered = query.Count();

            query = orderColumn switch
            {
                0 => orderDirection == "asc"
                    ? query.OrderBy(s => s.User != null ? s.User.FullName : string.Empty)
                    : query.OrderByDescending(s => s.User != null ? s.User.FullName : string.Empty),
                1 => orderDirection == "asc" ? query.OrderBy(s => s.OpenedAt) : query.OrderByDescending(s => s.OpenedAt),
                2 => orderDirection == "asc" ? query.OrderBy(s => s.ClosedAt) : query.OrderByDescending(s => s.ClosedAt),
                3 => orderDirection == "asc" ? query.OrderBy(s => s.OpeningAmount) : query.OrderByDescending(s => s.OpeningAmount),
                4 => orderDirection == "asc" ? query.OrderBy(s => s.ExpectedCashAmount) : query.OrderByDescending(s => s.ExpectedCashAmount),
                5 => orderDirection == "asc" ? query.OrderBy(s => s.CountedCashAmount) : query.OrderByDescending(s => s.CountedCashAmount),
                6 => orderDirection == "asc" ? query.OrderBy(s => s.Difference) : query.OrderByDescending(s => s.Difference),
                7 => orderDirection == "asc" ? query.OrderBy(s => s.ClosedAt == null) : query.OrderByDescending(s => s.ClosedAt == null),
                _ => query.OrderByDescending(s => s.OpenedAt)
            };

            var data = query
                .Skip(start)
                .Take(length)
                .ToList()
                .Select(session => new
            {
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
                isOpen = session.IsOpen
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
                .GetAll(s => s.CashRegisterSessionId == session.Id)
                .ToList();

            var accountPayments = _workContainer.PersonalAccountPayment
                .GetAll(p => p.CashRegisterSessionId == session.Id)
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

            return new CashRegisterSummaryVM
            {
                CashSales = cashSales,
                TransferSales = transferSales,
                PersonalAccountSales = personalAccountSales,
                PersonalAccountCashPayments = personalAccountCashPayments,
                PersonalAccountTransferPayments = personalAccountTransferPayments,
                TotalSales = sales.Sum(s => s.Total),
                ExpectedCash = session.OpeningAmount + cashSales + personalAccountCashPayments,
                SalesCount = sales.Count
            };
        }

        private string? GetCurrentUserId()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            return claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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

            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
        }
    }
}
