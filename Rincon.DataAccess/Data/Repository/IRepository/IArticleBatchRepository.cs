using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository.IRepository
{
    public interface IArticleBatchRepository : IRepository<ArticleBatch>
    {
        void Update(ArticleBatch articleBatch);
    }
}
