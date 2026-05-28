using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository.IRepository
{
    public interface ICashRegisterSessionRepository : IRepository<CashRegisterSession>
    {
        void Update(CashRegisterSession cashRegisterSession);
    }
}
