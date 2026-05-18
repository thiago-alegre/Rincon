using Microsoft.AspNetCore.Mvc.Rendering;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Rincon.DataAccess.Data.Repository
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        private readonly ApplicationDbContext _db;
        public CategoryRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public IEnumerable<SelectListItem> GetListCategory()
        {
            return _db.Categories.Select(i => new SelectListItem()
            {
                Text = i.Name,
                Value = i.Id.ToString()
            });
        }

        public void Update(Category category)
        {
            var objFromDb = _db.Categories.FirstOrDefault(s => s.Id == category.Id);
            if (objFromDb != null)
            {
                objFromDb.Name = category.Name;
                objFromDb.Date = category.Date;
                objFromDb.isActive = category.isActive;
                // _db.SaveChanges(); 
            }
            else
            {
                throw new Exception("Categoría no encontrada");
            }
        }

        IEnumerable<SelectListItem> ICategoryRepository.GetListCategory()
        {
            throw new NotImplementedException();
        }
    }
}
