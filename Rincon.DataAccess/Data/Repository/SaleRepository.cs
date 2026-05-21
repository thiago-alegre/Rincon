using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository
{
    public class SaleRepository : Repository<Sale>, ISaleRepository
    {
        private readonly ApplicationDbContext _db;

        public SaleRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Sale sale)
        {
            _db.Sales.Update(sale);
        }
    }
}