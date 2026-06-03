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

        public IActionResult Detail(int id)
        {
            var sale = _workContainer.Sale.GetFirstOrDefault(
                s => s.Id == id,
                includeProperties: "SaleDetails,User,CashRegisterSession,CashRegisterSession.User,PersonalAccount"
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
                var matchingPaymentMethods = Enum.GetValues<PaymentMethod>()
                    .Where(p => p.ToString().Contains(searchValue, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                query = query.Where(s =>
                    matchingPaymentMethods.Contains(s.PaymentMethod) ||
                    (s.User != null &&
                        ((s.User.FullName != null && s.User.FullName.Contains(searchValue)) ||
                         (s.User.Email != null && s.User.Email.Contains(searchValue)))));
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

        private int GetDataTablesInt(string key, int defaultValue = 0)
        {
            return int.TryParse(Request.Query[key], out var value) ? value : defaultValue;
        }
    }
}
