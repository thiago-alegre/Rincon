using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;
using Rincon.Models.ViewModels;
using Rincon.Utilities;

namespace Rincon.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class SalesController : Controller
    {
        private readonly IWorkContainer _workContainer;
        private readonly UserManager<ApplicationUser> _userManager;

        public SalesController(IWorkContainer workContainer, UserManager<ApplicationUser> userManager)
        {
            _workContainer = workContainer;
            _userManager = userManager;
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

        public IActionResult Detail(int id)
        {
            var sale = _workContainer.Sale.GetFirstOrDefault(
                s => s.Id == id,
                includeProperties: "SaleDetails,User,CashRegisterSession,CashRegisterSession.User"
            );

            if (sale == null)
            {
                return NotFound();
            }

            return View(sale);
        }

        [HttpGet]
        public IActionResult GetAll(string? userId, DateTime? saleDate)
        {
            var sales = _workContainer.Sale.GetAll(
                includeProperties: "User"
            );

            if (!string.IsNullOrWhiteSpace(userId))
            {
                sales = sales.Where(s => s.UserId == userId);
            }

            if (saleDate.HasValue)
            {
                var from = saleDate.Value.Date;
                var to = from.AddDays(1);
                sales = sales.Where(s => s.Date >= from && s.Date < to);
            }

            var salesData = sales
                .OrderByDescending(s => s.Date)
                .Select(s => new
                {
                    id = s.Id,
                    date = s.Date.ToString("dd/MM/yyyy HH:mm"),
                    total = s.Total.ToString("N2"),
                    paymentMethod = s.PaymentMethod.ToString(),
                    user = s.User != null
                        ? (!string.IsNullOrWhiteSpace(s.User.FullName) ? s.User.FullName : s.User.Email)
                        : "Sin usuario",
                    detailUrl = Url.Action("Detail", "Sales", new { area = "Admin", id = s.Id })
                });

            return Json(new { data = salesData });
        }
    }
}
