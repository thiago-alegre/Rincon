using System;

namespace Rincon.DataAccess.Data.Repository.IRepository
{
    public interface IWorkContainer : IDisposable
    {
        ICategoryRepository Category { get; }
        IArticleRepository Article { get; }
        ISaleRepository Sale { get; }
        ISaleDetailRepository SaleDetail { get; }
        ICashRegisterSessionRepository CashRegisterSession { get; }
        IPersonalAccountRepository PersonalAccount { get; }

        void Save();
    }
}
