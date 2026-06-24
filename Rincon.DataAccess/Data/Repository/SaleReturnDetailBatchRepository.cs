using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository
{
    public class SaleReturnDetailBatchRepository : Repository<SaleReturnDetailBatch>, ISaleReturnDetailBatchRepository
    {
        private readonly ApplicationDbContext _db;

        public SaleReturnDetailBatchRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(SaleReturnDetailBatch saleReturnDetailBatch)
        {
            _db.SaleReturnDetailBatches.Update(saleReturnDetailBatch);
        }
    }
}
