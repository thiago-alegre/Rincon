using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository.IRepository
{
    public interface IArticleRepository : IRepository<Article>
    {
        void Update(Article article);
    }
}
