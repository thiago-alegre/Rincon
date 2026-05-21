using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository.IRepository
{
    public interface ISaleDetailRepository : IRepository<SaleDetail>
    {
        void Update(SaleDetail saleDetail);
    }
}