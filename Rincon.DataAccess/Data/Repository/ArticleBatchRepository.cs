using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository
{
    public class ArticleBatchRepository : Repository<ArticleBatch>, IArticleBatchRepository
    {
        private readonly ApplicationDbContext _db;

        public ArticleBatchRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ArticleBatch articleBatch)
        {
            _db.ArticleBatches.Update(articleBatch);
        }
    }
}
