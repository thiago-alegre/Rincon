using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository
{
    public class SaleDetailBatchRepository : Repository<SaleDetailBatch>, ISaleDetailBatchRepository
    {
        private readonly ApplicationDbContext _db;

        public SaleDetailBatchRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(SaleDetailBatch saleDetailBatch)
        {
            _db.SaleDetailBatches.Update(saleDetailBatch);
        }
    }
}
