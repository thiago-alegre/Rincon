using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository
{
    public class SaleReturnRepository : Repository<SaleReturn>, ISaleReturnRepository
    {
        private readonly ApplicationDbContext _db;

        public SaleReturnRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(SaleReturn saleReturn)
        {
            _db.SaleReturns.Update(saleReturn);
        }
    }
}
