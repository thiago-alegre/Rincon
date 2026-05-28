using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public CashRegisterController(IWorkContainer workContainer)
        {
            _workContainer = workContainer;
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

            var cashSales = sales
                .Where(s => s.PaymentMethod == PaymentMethod.Efectivo)
                .Sum(s => s.Total);

            var transferSales = sales
                .Where(s => s.PaymentMethod == PaymentMethod.Transferencia)
                .Sum(s => s.Total);

            var personalAccountSales = sales
                .Where(s => s.PaymentMethod == PaymentMethod.CuentaPersonal)
                .Sum(s => s.Total);

            return new CashRegisterSummaryVM
            {
                CashSales = cashSales,
                TransferSales = transferSales,
                PersonalAccountSales = personalAccountSales,
                TotalSales = sales.Sum(s => s.Total),
                ExpectedCash = session.OpeningAmount + cashSales,
                SalesCount = sales.Count
            };
        }

        private string? GetCurrentUserId()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            return claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
