using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository
{
    public class SaleExchangeBatchRepository : Repository<SaleExchangeBatch>, ISaleExchangeBatchRepository
    {
        private readonly ApplicationDbContext _db;

        public SaleExchangeBatchRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(SaleExchangeBatch saleExchangeBatch)
        {
            _db.SaleExchangeBatches.Update(saleExchangeBatch);
        }
    }
}
