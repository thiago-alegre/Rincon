using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository.IRepository
{
    public interface IPersonalAccountPaymentRepository : IRepository<PersonalAccountPayment>
    {
        void Update(PersonalAccountPayment personalAccountPayment);
    }
}
