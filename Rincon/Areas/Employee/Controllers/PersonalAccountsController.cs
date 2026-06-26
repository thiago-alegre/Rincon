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
    public class PersonalAccountsController : Controller
    {
        private readonly IWorkContainer _workContainer;
        private readonly ApplicationDbContext _db;

        public PersonalAccountsController(IWorkContainer workContainer, ApplicationDbContext db)
        {
            _workContainer = workContainer;
            _db = db;
        }

        public IActionResult Index()
        {
            ViewBag.CanManage = User.IsInRole(SD.Role_Admin);
            return View();
        }

        public IActionResult Detail(int id)
        {
            var account = _workContainer.PersonalAccount.GetFirstOrDefault(a => a.Id == id);

            if (account == null)
            {
                return NotFound();
            }

            var pendingSales = _db.Sales
                .AsNoTracking()
                .Where(s => !s.IsVoided && s.PaymentMethod == PaymentMethod.CuentaPersonal && s.Total > s.PersonalAccountPaidAmount)
                .Where(s => s.PersonalAccountId == id)
                .ToList();

            var vm = new PersonalAccountDetailVM
            {
                Account = account,
                CurrentDebt = pendingSales.Sum(s => s.Total - s.PersonalAccountPaidAmount),
                DebtSince = pendingSales.OrderBy(s => s.Date).FirstOrDefault()?.Date
            };

            return View(vm);
        }

        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult Upsert(int? id)
        {
            if (id == null || id == 0)
            {
                return View(new PersonalAccount());
            }

            var account = _workContainer.PersonalAccount.Get(id.Value);

            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult Upsert(PersonalAccount account)
        {
            if (!ModelState.IsValid)
            {
                return View(account);
            }

            if (account.Id == 0)
            {
                _workContainer.PersonalAccount.Add(account);
                TempData["success"] = "Cuenta personal creada correctamente";
            }
            else
            {
                _workContainer.PersonalAccount.Update(account);
                TempData["success"] = "Cuenta personal actualizada correctamente";
            }

            _workContainer.Save();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Settle(PersonalAccountSettleVM vm)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["error"] = "No se pudo identificar el usuario actual";
                return RedirectToAction(nameof(Detail), new { id = vm.Id });
            }

            var openCashRegister = _workContainer.CashRegisterSession.GetFirstOrDefault(
                s => s.UserId == userId && s.ClosedAt == null);

            if (openCashRegister == null)
            {
                TempData["error"] = "Debe abrir una caja antes de saldar cuentas personales";
                return RedirectToAction(nameof(Detail), new { id = vm.Id });
            }

            if (vm.PaymentMethod != PaymentMethod.Efectivo && vm.PaymentMethod != PaymentMethod.Transferencia)
            {
                TempData["error"] = "Seleccione un medio de pago válido";
                return RedirectToAction(nameof(Detail), new { id = vm.Id });
            }

            var sales = _workContainer.Sale
                .GetAll(s => s.PersonalAccountId == vm.Id && !s.IsVoided && s.PaymentMethod == PaymentMethod.CuentaPersonal && s.Total > s.PersonalAccountPaidAmount)
                .OrderBy(s => s.Date)
                .ToList();

            var amount = sales.Sum(s => s.Total);
            var pendingAmount = sales.Sum(s => s.Total - s.PersonalAccountPaidAmount);

            if (pendingAmount <= 0)
            {
                TempData["error"] = "La cuenta no tiene deuda pendiente";
                return RedirectToAction(nameof(Detail), new { id = vm.Id });
            }

            decimal paymentAmount = pendingAmount;

            if (!string.IsNullOrWhiteSpace(vm.AmountText))
            {
                if (!TryParseDecimal(vm.AmountText, out paymentAmount) || paymentAmount <= 0)
                {
                    TempData["error"] = "Ingrese un monto abonado válido";
                    return RedirectToAction(nameof(Detail), new { id = vm.Id });
                }

                if (paymentAmount > pendingAmount)
                {
                    TempData["error"] = $"El monto abonado no puede superar la deuda pendiente de $ {pendingAmount:N2}";
                    return RedirectToAction(nameof(Detail), new { id = vm.Id });
                }
            }

            _workContainer.PersonalAccountPayment.Add(new PersonalAccountPayment
            {
                Date = DateTime.Now,
                Amount = paymentAmount,
                PaymentMethod = vm.PaymentMethod,
                Notes = vm.Notes,
                PersonalAccountId = vm.Id,
                CashRegisterSessionId = openCashRegister.Id,
                UserId = userId
            });

            var remainingPayment = paymentAmount;

            foreach (var sale in sales)
            {
                if (remainingPayment <= 0)
                {
                    break;
                }

                var salePendingAmount = sale.Total - sale.PersonalAccountPaidAmount;
                var amountApplied = Math.Min(salePendingAmount, remainingPayment);

                sale.PersonalAccountPaidAmount += amountApplied;
                remainingPayment -= amountApplied;

                if (sale.PersonalAccountPaidAmount >= sale.Total)
                {
                    sale.IsPersonalAccountSettled = true;
                    sale.PersonalAccountSettledAt = DateTime.Now;
                }
                else
                {
                    sale.IsPersonalAccountSettled = false;
                    sale.PersonalAccountSettledAt = null;
                }

                _workContainer.Sale.Update(sale);
            }

            _workContainer.Save();

            var remainingDebt = pendingAmount - paymentAmount;

            TempData["modalTitle"] = remainingDebt <= 0
                ? "Cuenta personal saldada"
                : "Pago parcial registrado";
            TempData["modalText"] = remainingDebt <= 0
                ? $"Se registró un pago de $ {paymentAmount:N2}. La cuenta quedó sin deuda pendiente."
                : $"Se registró un pago de $ {paymentAmount:N2}. Deuda restante: $ {remainingDebt:N2}.";
            TempData["modalIcon"] = "success";
            TempData["modalConfirmText"] = "Entendido";
            return RedirectToAction(nameof(Detail), new { id = vm.Id });
        }

        [HttpGet]
        public IActionResult Search(string? term, int page = 1)
        {
            const int pageSize = 10;

            page = page < 1 ? 1 : page;

            var query = _db.PersonalAccounts
                .AsNoTracking()
                .Where(a => a.isActive);

            if (!string.IsNullOrWhiteSpace(term))
            {
                var search = $"%{term.Trim()}%";

                query = query.Where(a =>
                    EF.Functions.ILike(a.FullName, search) ||
                    EF.Functions.ILike(a.DNI, search) ||
                    (a.Phone != null && EF.Functions.ILike(a.Phone, search)) ||
                    (a.Address != null && EF.Functions.ILike(a.Address, search)));
            }

            var accounts = query
                .OrderBy(a => a.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize + 1)
                .Select(a => new
                {
                    a.Id,
                    a.FullName,
                    a.DNI,
                    a.Phone
                })
                .ToList();

            return Json(new
            {
                results = accounts.Take(pageSize).Select(a => new
                {
                    id = a.Id,
                    text = $"{a.FullName} - DNI {a.DNI}" + (string.IsNullOrWhiteSpace(a.Phone) ? "" : $" - Tel. {a.Phone}")
                }),
                pagination = new
                {
                    more = accounts.Count > pageSize
                }
            });
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var draw = GetDataTablesInt("draw");
            var start = GetDataTablesInt("start");
            var length = GetDataTablesInt("length", 10);
            var searchValue = Request.Query["search[value]"].ToString()?.Trim();
            var orderColumn = GetDataTablesInt("order[0][column]");
            var orderDirection = Request.Query["order[0][dir]"].ToString();

            var query = _db.PersonalAccounts
                .AsNoTracking()
                .Where(a => a.isActive)
                .Select(a => new
                {
                    id = a.Id,
                    fullName = a.FullName,
                    dni = a.DNI,
                    address = a.Address,
                    phone = a.Phone,
                    debtValue = _db.Sales
                        .Where(s => s.PersonalAccountId == a.Id && !s.IsVoided && s.PaymentMethod == PaymentMethod.CuentaPersonal && s.Total > s.PersonalAccountPaidAmount)
                        .Sum(s => (decimal?)(s.Total - s.PersonalAccountPaidAmount)) ?? 0m,
                    debtSinceValue = _db.Sales
                        .Where(s => s.PersonalAccountId == a.Id && !s.IsVoided && s.PaymentMethod == PaymentMethod.CuentaPersonal && s.Total > s.PersonalAccountPaidAmount)
                        .Min(s => (DateTime?)s.Date)
                });

            var recordsTotal = query.Count();

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                var searchPattern = $"%{searchValue}%";

                query = query.Where(a =>
                    EF.Functions.ILike(a.fullName, searchPattern) ||
                    EF.Functions.ILike(a.dni, searchPattern) ||
                    (a.phone != null && EF.Functions.ILike(a.phone, searchPattern)) ||
                    (a.address != null && EF.Functions.ILike(a.address, searchPattern)));
            }

            var recordsFiltered = query.Count();

            query = orderColumn switch
            {
                0 => orderDirection == "asc" ? query.OrderBy(a => a.fullName) : query.OrderByDescending(a => a.fullName),
                1 => orderDirection == "asc" ? query.OrderBy(a => a.dni) : query.OrderByDescending(a => a.dni),
                2 => orderDirection == "asc" ? query.OrderBy(a => a.phone) : query.OrderByDescending(a => a.phone),
                3 => orderDirection == "asc" ? query.OrderBy(a => a.address) : query.OrderByDescending(a => a.address),
                4 => orderDirection == "asc" ? query.OrderBy(a => a.debtValue) : query.OrderByDescending(a => a.debtValue),
                5 => orderDirection == "asc" ? query.OrderBy(a => a.debtSinceValue) : query.OrderByDescending(a => a.debtSinceValue),
                _ => query.OrderBy(a => a.fullName)
            };

            var canManage = User.IsInRole(SD.Role_Admin);
            var data = query
                .Skip(start)
                .Take(length)
                .ToList()
                .Select(account => new
                {
                    account.id,
                    account.fullName,
                    account.dni,
                    account.address,
                    account.phone,
                    debt = account.debtValue.ToString("N2"),
                    account.debtValue,
                    debtSince = account.debtSinceValue.HasValue ? account.debtSinceValue.Value.ToString("dd/MM/yyyy") : "-",
                    detailUrl = Url.Action("Detail", "PersonalAccounts", new { area = "Employee", id = account.id }),
                    editUrl = Url.Action("Upsert", "PersonalAccounts", new { area = "Employee", id = account.id }),
                    canManage
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
        public IActionResult GetSaleDetails(int id)
        {
            var draw = GetDataTablesInt("draw");
            var start = GetDataTablesInt("start");
            var length = GetDataTablesInt("length", 5);
            var searchValue = Request.Query["search[value]"].ToString()?.Trim();
            var orderColumn = GetDataTablesInt("order[0][column]");
            var orderDirection = Request.Query["order[0][dir]"].ToString();

            var query = _db.SaleDetails
                .AsNoTracking()
                .Include(d => d.Sale)
                .Where(d => d.Sale.PersonalAccountId == id)
                .Select(d => new
                {
                    saleDate = d.Sale.Date,
                    d.ArticleName,
                    d.Quantity,
                    d.UnitOfMeasure,
                    d.UnitPrice,
                    d.Subtotal,
                    d.Sale.PersonalAccountPaidAmount,
                    d.Sale.IsPersonalAccountSettled,
                    d.Sale.IsVoided
                });

            var recordsTotal = query.Count();

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                var search = searchValue.ToLower();
                var searchPattern = $"%{searchValue}%";
                var settledSearch = "saldada".Contains(search);
                var pendingSearch = "pendiente".Contains(search);
                var voidedSearch = "anulada".Contains(search);

                query = query.Where(d =>
                    EF.Functions.ILike(d.ArticleName, searchPattern) ||
                    EF.Functions.ILike(d.UnitOfMeasure, searchPattern) ||
                    (voidedSearch && d.IsVoided) ||
                    (settledSearch && d.IsPersonalAccountSettled) ||
                    (pendingSearch && !d.IsPersonalAccountSettled && !d.IsVoided));
            }

            var recordsFiltered = query.Count();

            query = orderColumn switch
            {
                0 => orderDirection == "asc" ? query.OrderBy(d => d.saleDate) : query.OrderByDescending(d => d.saleDate),
                1 => orderDirection == "asc" ? query.OrderBy(d => d.ArticleName) : query.OrderByDescending(d => d.ArticleName),
                2 => orderDirection == "asc" ? query.OrderBy(d => d.Quantity) : query.OrderByDescending(d => d.Quantity),
                3 => orderDirection == "asc" ? query.OrderBy(d => d.UnitPrice) : query.OrderByDescending(d => d.UnitPrice),
                4 => orderDirection == "asc" ? query.OrderBy(d => d.Subtotal) : query.OrderByDescending(d => d.Subtotal),
                5 => orderDirection == "asc" ? query.OrderBy(d => d.IsPersonalAccountSettled) : query.OrderByDescending(d => d.IsPersonalAccountSettled),
                _ => query.OrderByDescending(d => d.saleDate)
            };

            var data = query
                .Skip(start)
                .Take(length)
                .ToList()
                .Select(d =>
                {
                    var status = GetPersonalAccountSaleDetailStatus(
                        d.IsVoided,
                        d.IsPersonalAccountSettled);

                    return new
                    {
                        date = d.saleDate.ToString("dd/MM/yyyy HH:mm"),
                        product = d.ArticleName,
                        quantity = FormatSaleQuantity(d.Quantity, d.UnitOfMeasure),
                        unitPrice = d.UnitPrice.ToString("N2"),
                        subtotal = d.Subtotal.ToString("N2"),
                        status = status.Text,
                        statusClass = status.ClassName,
                        settled = d.IsPersonalAccountSettled,
                        paidAmount = d.PersonalAccountPaidAmount.ToString("N2")
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
        public IActionResult GetPayments(int id)
        {
            var draw = GetDataTablesInt("draw");
            var start = GetDataTablesInt("start");
            var length = GetDataTablesInt("length", 5);
            var searchValue = Request.Query["search[value]"].ToString()?.Trim();
            var orderColumn = GetDataTablesInt("order[0][column]");
            var orderDirection = Request.Query["order[0][dir]"].ToString();

            var query = _db.PersonalAccountPayments
                .AsNoTracking()
                .Include(p => p.User)
                .Where(p => p.PersonalAccountId == id)
                .Select(p => new
                {
                    p.Date,
                    p.PaymentMethod,
                    p.Amount,
                    p.CashRegisterSessionId,
                    userName = p.User != null
                        ? (p.User.FullName != null && p.User.FullName != string.Empty ? p.User.FullName : p.User.Email)
                        : "Sin usuario",
                    p.Notes
                });

            var recordsTotal = query.Count();

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                var searchPattern = $"%{searchValue}%";
                var matchingPaymentMethods = Enum.GetValues<PaymentMethod>()
                    .Where(p => p.ToString().Contains(searchValue, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                query = query.Where(p =>
                    matchingPaymentMethods.Contains(p.PaymentMethod) ||
                    (p.userName != null && EF.Functions.ILike(p.userName, searchPattern)) ||
                    (p.Notes != null && EF.Functions.ILike(p.Notes, searchPattern)));
            }

            var recordsFiltered = query.Count();

            query = orderColumn switch
            {
                0 => orderDirection == "asc" ? query.OrderBy(p => p.Date) : query.OrderByDescending(p => p.Date),
                1 => orderDirection == "asc" ? query.OrderBy(p => p.PaymentMethod) : query.OrderByDescending(p => p.PaymentMethod),
                2 => orderDirection == "asc" ? query.OrderBy(p => p.Amount) : query.OrderByDescending(p => p.Amount),
                3 => orderDirection == "asc" ? query.OrderBy(p => p.CashRegisterSessionId) : query.OrderByDescending(p => p.CashRegisterSessionId),
                4 => orderDirection == "asc" ? query.OrderBy(p => p.userName) : query.OrderByDescending(p => p.userName),
                5 => orderDirection == "asc" ? query.OrderBy(p => p.Notes) : query.OrderByDescending(p => p.Notes),
                _ => query.OrderByDescending(p => p.Date)
            };

            var data = query
                .Skip(start)
                .Take(length)
                .ToList()
                .Select(p => new
                {
                    date = p.Date.ToString("dd/MM/yyyy HH:mm"),
                    paymentMethod = p.PaymentMethod.ToString(),
                    amount = p.Amount.ToString("N2"),
                    cashRegister = $"Caja #{p.CashRegisterSessionId}",
                    user = p.userName,
                    notes = string.IsNullOrWhiteSpace(p.Notes) ? "-" : p.Notes
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
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult Delete(int id)
        {
            var account = _workContainer.PersonalAccount.Get(id);

            if (account == null)
            {
                return Json(new { success = false, message = "No se encontró la cuenta personal" });
            }

            var hasDebt = _workContainer.Sale
                .GetAll(s => s.PersonalAccountId == id && !s.IsVoided && s.PaymentMethod == PaymentMethod.CuentaPersonal && !s.IsPersonalAccountSettled)
                .Any();

            if (hasDebt)
            {
                return Json(new { success = false, message = "No se puede eliminar una cuenta con deuda pendiente" });
            }

            account.isActive = false;
            _workContainer.PersonalAccount.Update(account);
            _workContainer.Save();

            return Json(new { success = true, message = "Cuenta personal eliminada correctamente" });
        }

        private string? GetCurrentUserId()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            return claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private int GetDataTablesInt(string key, int defaultValue = 0)
        {
            return int.TryParse(Request.Query[key], out var value) ? value : defaultValue;
        }

        private bool TryParseDecimal(string? value, out decimal result) => DecimalParser.TryParse(value, out result);

        private static string FormatSaleQuantity(decimal quantity, string unitOfMeasure)
        {
            if (unitOfMeasure == "Kilogramo")
            {
                var formatted = quantity % 1 == 0
                    ? quantity.ToString("N0")
                    : quantity.ToString("N3").TrimEnd('0').TrimEnd(',');

                return $"{formatted} kg";
            }

            return $"{quantity:N0}";
        }

        private static (string Text, string ClassName) GetPersonalAccountSaleDetailStatus(
            bool isVoided,
            bool isSettled)
        {
            if (isVoided)
            {
                return ("Anulada", "status-inactive");
            }

            return isSettled
                ? ("Saldada", "status-active")
                : ("Pendiente", "status-inactive");
        }
    }
}
