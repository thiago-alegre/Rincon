using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository.IRepository
{
    public interface ISaleExchangeRepository : IRepository<SaleExchange>
    {
        void Update(SaleExchange saleExchange);
    }
}
