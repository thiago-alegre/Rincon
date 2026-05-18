using Microsoft.AspNetCore.Mvc.Rendering;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Rincon.DataAccess.Data.Repository.IRepository
{
    public interface ICategoryRepository : IRepository<Category>
    {
        void Update(Category category);
        public IEnumerable<SelectListItem> GetListCategory();

    }
}
