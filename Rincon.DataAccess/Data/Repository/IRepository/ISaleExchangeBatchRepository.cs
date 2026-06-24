using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository.IRepository
{
    public interface ISaleExchangeBatchRepository : IRepository<SaleExchangeBatch>
    {
        void Update(SaleExchangeBatch saleExchangeBatch);
    }
}
