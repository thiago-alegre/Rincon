using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository.IRepository
{
    public interface ISaleReturnDetailRepository : IRepository<SaleReturnDetail>
    {
        void Update(SaleReturnDetail saleReturnDetail);
    }
}
