using System;

namespace Rincon.DataAccess.Data.Repository.IRepository
{
    public interface IWorkContainer : IDisposable
    {
        ICategoryRepository Category { get; }
        IArticleRepository Article { get; }
        IArticleBatchRepository ArticleBatch { get; }
        ISaleRepository Sale { get; }
        ISaleDetailRepository SaleDetail { get; }
        ISaleDetailBatchRepository SaleDetailBatch { get; }
        ISaleReturnRepository SaleReturn { get; }
        ISaleReturnDetailRepository SaleReturnDetail { get; }
        ISaleReturnDetailBatchRepository SaleReturnDetailBatch { get; }
        ISaleExchangeRepository SaleExchange { get; }
        ISaleExchangeBatchRepository SaleExchangeBatch { get; }
        ICashRegisterSessionRepository CashRegisterSession { get; }
        IPersonalAccountRepository PersonalAccount { get; }
        IPersonalAccountPaymentRepository PersonalAccountPayment { get; }

        void Save();
    }
}
