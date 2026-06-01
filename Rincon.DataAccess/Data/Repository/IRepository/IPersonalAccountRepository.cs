using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository.IRepository
{
    public interface IPersonalAccountRepository : IRepository<PersonalAccount>
    {
        void Update(PersonalAccount personalAccount);
    }
}
