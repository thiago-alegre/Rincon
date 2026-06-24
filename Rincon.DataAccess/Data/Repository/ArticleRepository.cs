using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository
{
    public class ArticleRepository : Repository<Article>, IArticleRepository
    {
        private readonly ApplicationDbContext _db;

        public ArticleRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Article article)
        {
            var objFromDb = _db.Articles.FirstOrDefault(a => a.Id == article.Id);

            if (objFromDb != null)
            {
                objFromDb.Name = article.Name;
                objFromDb.Description = article.Description;
                objFromDb.Code = article.Code;
                objFromDb.Price = article.Price;
                objFromDb.Cost = article.Cost;
                objFromDb.Stock = article.Stock;
                objFromDb.StockMin = article.StockMin;
                objFromDb.UsesBatches = article.UsesBatches;
                objFromDb.IsSoldByWeight = article.IsSoldByWeight;
                objFromDb.UnitOfMeasure = article.UnitOfMeasure;
                objFromDb.ExpirationDate = article.ExpirationDate;
                objFromDb.CategoryId = article.CategoryId;
                objFromDb.isActive = article.isActive;

                if (!string.IsNullOrEmpty(article.ImageUrl))
                {
                    objFromDb.ImageUrl = article.ImageUrl;
                }
            }
        }
    }
}
