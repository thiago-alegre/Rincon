using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository.IRepository
{
    public interface ISaleReturnRepository : IRepository<SaleReturn>
    {
        void Update(SaleReturn saleReturn);
    }
}
