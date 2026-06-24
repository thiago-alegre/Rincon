using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository
{
    public class SaleReturnDetailRepository : Repository<SaleReturnDetail>, ISaleReturnDetailRepository
    {
        private readonly ApplicationDbContext _db;

        public SaleReturnDetailRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(SaleReturnDetail saleReturnDetail)
        {
            _db.SaleReturnDetails.Update(saleReturnDetail);
        }
    }
}
