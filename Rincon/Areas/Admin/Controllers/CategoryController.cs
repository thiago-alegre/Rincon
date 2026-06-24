using Microsoft.AspNetCore.Mvc;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Rincon.DataAccess.Data;
using Rincon.Utilities;

namespace Rincon.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]

    public class CategoryController : Controller
    {
        private readonly IWorkContainer _workContainer;
        private readonly ApplicationDbContext _db;

        public CategoryController(IWorkContainer workContainer, ApplicationDbContext db)
        {
            _workContainer = workContainer;
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            Category category = new Category();

            if (id == null)
            {
                return View(category);
            }

            category = _workContainer.Category.Get(id.GetValueOrDefault());

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Category category)
        {
            if (ModelState.IsValid)
            {
                if (category.Id == 0)
                {
                    _workContainer.Category.Add(category);
                    TempData["success"] = "Categoría creada correctamente";
                }
                else
                {
                    _workContainer.Category.Update(category);
                    TempData["success"] = "Categoría actualizada correctamente";
                }

                _workContainer.Save();

                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            var draw = GetDataTablesInt("draw");
            var start = GetDataTablesInt("start");
            var length = GetDataTablesInt("length", 5);
            var searchValue = Request.Query["search[value]"].ToString()?.Trim();
            var orderColumn = GetDataTablesInt("order[0][column]");
            var orderDirection = Request.Query["order[0][dir]"].ToString();

            var query = _db.Categories.AsNoTracking().AsQueryable();
            var recordsTotal = query.Count();

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                query = query.Where(c => c.Name.Contains(searchValue));
            }

            var recordsFiltered = query.Count();

            query = orderColumn switch
            {
                0 => orderDirection == "asc" ? query.OrderBy(c => c.Name) : query.OrderByDescending(c => c.Name),
                1 => orderDirection == "asc" ? query.OrderBy(c => c.Date) : query.OrderByDescending(c => c.Date),
                2 => orderDirection == "asc" ? query.OrderBy(c => c.isActive) : query.OrderByDescending(c => c.isActive),
                _ => query.OrderBy(c => c.Name)
            };

            var categories = query
                .Skip(start)
                .Take(length)
                .ToList();

            return Json(new
            {
                draw,
                recordsTotal,
                recordsFiltered,
                data = categories
            });
        }

        [HttpGet]
        public IActionResult Search(string? term, int page = 1)
        {
            const int pageSize = 10;

            page = page < 1 ? 1 : page;

            var categories = _db.Categories
                .AsNoTracking()
                .Where(c => c.isActive);

            if (!string.IsNullOrWhiteSpace(term))
            {
                categories = categories.Where(c => c.Name.Contains(term));
            }

            var pagedCategories = categories
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize + 1)
                .ToList();

            return Json(new
            {
                results = pagedCategories.Take(pageSize).Select(c => new
                {
                    id = c.Id,
                    text = c.Name
                }),
                pagination = new
                {
                    more = pagedCategories.Count > pageSize
                }
            });
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var objFromDb = _workContainer.Category.Get(id);

            if (objFromDb == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Error al eliminar la categoría"
                });
            }

            _workContainer.Category.Remove(objFromDb);
            _workContainer.Save();

            return Json(new
            {
                success = true,
                message = "Categoría eliminada correctamente"
            });
        }

        #endregion

        private int GetDataTablesInt(string key, int defaultValue = 0)
        {
            return int.TryParse(Request.Query[key], out var value) ? value : defaultValue;
        }
    }
}
