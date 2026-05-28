using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository
{
    public class CashRegisterSessionRepository : Repository<CashRegisterSession>, ICashRegisterSessionRepository
    {
        private readonly ApplicationDbContext _db;

        public CashRegisterSessionRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(CashRegisterSession cashRegisterSession)
        {
            _db.CashRegisterSessions.Update(cashRegisterSession);
        }
    }
}
