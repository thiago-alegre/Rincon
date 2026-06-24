using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository
{
    public class SaleExchangeRepository : Repository<SaleExchange>, ISaleExchangeRepository
    {
        private readonly ApplicationDbContext _db;

        public SaleExchangeRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(SaleExchange saleExchange)
        {
            _db.SaleExchanges.Update(saleExchange);
        }
    }
}
