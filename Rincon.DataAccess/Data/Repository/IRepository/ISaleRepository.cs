using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository.IRepository
{
    public interface ISaleRepository : IRepository<Sale>
    {
        void Update(Sale sale);
    }
}