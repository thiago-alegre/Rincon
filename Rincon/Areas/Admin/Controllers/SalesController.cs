using Microsoft.AspNetCore.Mvc;
using Rincon.DataAccess.Data.Repository.IRepository;

namespace Rincon.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SalesController : Controller
    {
        private readonly IWorkContainer _workContainer;

        public SalesController(IWorkContainer workContainer)
        {
            _workContainer = workContainer;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Detail(int id)
        {
            var sale = _workContainer.Sale.GetFirstOrDefault(
                s => s.Id == id,
                includeProperties: "SaleDetails,User"
            );

            if (sale == null)
            {
                return NotFound();
            }

            return View(sale);
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var sales = _workContainer.Sale.GetAll(
                includeProperties: "User"
            )
            .OrderByDescending(s => s.Date)
            .Select(s => new
            {
                id = s.Id,
                date = s.Date.ToString("dd/MM/yyyy HH:mm"),
                total = s.Total.ToString("N2"),
                paymentMethod = s.PaymentMethod.ToString(),
                user = s.User != null ? s.User.Email : "Sin usuario",
                detailUrl = Url.Action("Detail", "Sales", new { area = "Admin", id = s.Id })
            });

            return Json(new { data = sales });
        }
    }
}