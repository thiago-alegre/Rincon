using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;
using Rincon.Models.ViewModels;
using Rincon.Utilities;
using Rincon.Utilities.Enums;

namespace Rincon.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
    public class PersonalAccountsController : Controller
    {
        private readonly IWorkContainer _workContainer;

        public PersonalAccountsController(IWorkContainer workContainer)
        {
            _workContainer = workContainer;
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

            var sales = _workContainer.Sale
                .GetAll(
                    s => s.PersonalAccountId == id,
                    orderBy: q => q.OrderByDescending(s => s.Date),
                    includeProperties: "SaleDetails,User")
                .ToList();

            var pendingSales = sales
                .Where(s => s.PaymentMethod == PaymentMethod.CuentaPersonal && !s.IsPersonalAccountSettled)
                .ToList();

            var vm = new PersonalAccountDetailVM
            {
                Account = account,
                Sales = sales,
                CurrentDebt = pendingSales.Sum(s => s.Total),
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
        public IActionResult Settle(int id)
        {
            var sales = _workContainer.Sale
                .GetAll(s => s.PersonalAccountId == id && s.PaymentMethod == PaymentMethod.CuentaPersonal && !s.IsPersonalAccountSettled)
                .ToList();

            foreach (var sale in sales)
            {
                sale.IsPersonalAccountSettled = true;
                sale.PersonalAccountSettledAt = DateTime.Now;
                _workContainer.Sale.Update(sale);
            }

            _workContainer.Save();

            TempData["success"] = "Cuenta personal saldada correctamente";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var accounts = _workContainer.PersonalAccount
                .GetAll(a => a.isActive)
                .OrderBy(a => a.FullName)
                .ToList();

            var pendingSales = _workContainer.Sale
                .GetAll(s => s.PaymentMethod == PaymentMethod.CuentaPersonal && !s.IsPersonalAccountSettled)
                .ToList();

            var data = accounts.Select(account =>
            {
                var accountSales = pendingSales
                    .Where(s => s.PersonalAccountId == account.Id)
                    .ToList();

                var debt = accountSales.Sum(s => s.Total);
                var debtSince = accountSales.OrderBy(s => s.Date).FirstOrDefault()?.Date;

                return new
                {
                    id = account.Id,
                    fullName = account.FullName,
                    dni = account.DNI,
                    address = account.Address,
                    phone = account.Phone,
                    debt = debt.ToString("N2"),
                    debtValue = debt,
                    debtSince = debtSince.HasValue ? debtSince.Value.ToString("dd/MM/yyyy") : "-",
                    detailUrl = Url.Action("Detail", "PersonalAccounts", new { area = "Employee", id = account.Id }),
                    editUrl = Url.Action("Upsert", "PersonalAccounts", new { area = "Employee", id = account.Id }),
                    canManage = User.IsInRole(SD.Role_Admin)
                };
            });

            return Json(new { data });
        }

        [HttpDelete]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult Delete(int id)
        {
            var account = _workContainer.PersonalAccount.Get(id);

            if (account == null)
            {
                return Json(new { success = false, message = "No se encontró la cuenta personal" });
            }

            var hasDebt = _workContainer.Sale
                .GetAll(s => s.PersonalAccountId == id && s.PaymentMethod == PaymentMethod.CuentaPersonal && !s.IsPersonalAccountSettled)
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
    }
}
