using Rincon.DataAccess.Data;
using Rincon.DataAccess.Data.Repository.IRepository;

namespace Rincon.DataAccess.Data.Repository
{
    public class WorkContainer : IWorkContainer
    {
        private readonly ApplicationDbContext _db;

        public ICategoryRepository Category { get; private set; }
        public IArticleRepository Article { get; private set; }
        public IArticleBatchRepository ArticleBatch { get; private set; }
        public ISaleRepository Sale { get; private set; }
        public ISaleDetailRepository SaleDetail { get; private set; }
        public ISaleDetailBatchRepository SaleDetailBatch { get; private set; }
        public ISaleReturnRepository SaleReturn { get; private set; }
        public ISaleReturnDetailRepository SaleReturnDetail { get; private set; }
        public ISaleReturnDetailBatchRepository SaleReturnDetailBatch { get; private set; }
        public ISaleExchangeRepository SaleExchange { get; private set; }
        public ISaleExchangeBatchRepository SaleExchangeBatch { get; private set; }
        public ICashRegisterSessionRepository CashRegisterSession { get; private set; }
        public IPersonalAccountRepository PersonalAccount { get; private set; }
        public IPersonalAccountPaymentRepository PersonalAccountPayment { get; private set; }

        public WorkContainer(ApplicationDbContext db)
        {
            _db = db;

            Category = new CategoryRepository(_db);
            Article = new ArticleRepository(_db);
            ArticleBatch = new ArticleBatchRepository(_db);
            Sale = new SaleRepository(_db);
            SaleDetail = new SaleDetailRepository(_db);
            SaleDetailBatch = new SaleDetailBatchRepository(_db);
            SaleReturn = new SaleReturnRepository(_db);
            SaleReturnDetail = new SaleReturnDetailRepository(_db);
            SaleReturnDetailBatch = new SaleReturnDetailBatchRepository(_db);
            SaleExchange = new SaleExchangeRepository(_db);
            SaleExchangeBatch = new SaleExchangeBatchRepository(_db);
            CashRegisterSession = new CashRegisterSessionRepository(_db);
            PersonalAccount = new PersonalAccountRepository(_db);
            PersonalAccountPayment = new PersonalAccountPaymentRepository(_db);
        }

        public void Save()
        {
            _db.SaveChanges();
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
