using Rincon.DataAccess.Data;
using Rincon.DataAccess.Data.Repository.IRepository;

namespace Rincon.DataAccess.Data.Repository
{
    public class WorkContainer : IWorkContainer
    {
        private readonly ApplicationDbContext _db;

        public ICategoryRepository Category { get; private set; }
        public IArticleRepository Article { get; private set; }
        public ISaleRepository Sale { get; private set; }
        public ISaleDetailRepository SaleDetail { get; private set; }

        public WorkContainer(ApplicationDbContext db)
        {
            _db = db;

            Category = new CategoryRepository(_db);
            Article = new ArticleRepository(_db);
            Sale = new SaleRepository(_db);
            SaleDetail = new SaleDetailRepository(_db);
        }

        public void Save()
        {
            _db.SaveChanges();
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}