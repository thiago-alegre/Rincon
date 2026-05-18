using Microsoft.AspNetCore.Mvc;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;

namespace Rincon.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly IWorkContainer _workContainer;

        public CategoryController(IWorkContainer workContainer)
        {
            _workContainer = workContainer;
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
            return Json(new
            {
                data = _workContainer.Category.GetAll()
            });
        }

        [HttpDelete]
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
    }
}