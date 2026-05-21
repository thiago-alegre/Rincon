using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository
{
    public class SaleDetailRepository : Repository<SaleDetail>, ISaleDetailRepository
    {
        private readonly ApplicationDbContext _db;

        public SaleDetailRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(SaleDetail saleDetail)
        {
            _db.SaleDetails.Update(saleDetail);
        }
    }
}