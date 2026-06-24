using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository.IRepository
{
    public interface ISaleReturnDetailBatchRepository : IRepository<SaleReturnDetailBatch>
    {
        void Update(SaleReturnDetailBatch saleReturnDetailBatch);
    }
}
