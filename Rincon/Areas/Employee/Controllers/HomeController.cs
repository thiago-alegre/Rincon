using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;
using Rincon.Utilities;

namespace Rincon.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWorkContainer _workContainer;

        public HomeController(ILogger<HomeController> logger, IWorkContainer workContainer)
        {
            _logger = logger;
            _workContainer = workContainer;
        }

        public IActionResult Index(string? searchString)
        {
            ViewBag.CurrentSearch = searchString;

            var articles = _workContainer.Article.GetAll(
                a => a.isActive == true,
                includeProperties: "Category"
            );

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                string normalizedSearch = searchString.Trim().ToLower();

                articles = articles.Where(a =>
                    a.Name.ToLower().Contains(normalizedSearch) ||
                    a.Code.ToLower().Contains(normalizedSearch) ||
                    (a.Category != null && a.Category.Name.ToLower().Contains(normalizedSearch))
                );
            }

            return View(articles);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
