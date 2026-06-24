using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository.IRepository
{
    public interface ISaleDetailBatchRepository : IRepository<SaleDetailBatch>
    {
        void Update(SaleDetailBatch saleDetailBatch);
    }
}
