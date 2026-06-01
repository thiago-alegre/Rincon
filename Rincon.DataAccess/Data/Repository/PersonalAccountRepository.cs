using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository
{
    public class PersonalAccountRepository : Repository<PersonalAccount>, IPersonalAccountRepository
    {
        private readonly ApplicationDbContext _db;

        public PersonalAccountRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(PersonalAccount personalAccount)
        {
            var accountFromDb = _db.PersonalAccounts.FirstOrDefault(a => a.Id == personalAccount.Id);

            if (accountFromDb != null)
            {
                accountFromDb.FullName = personalAccount.FullName;
                accountFromDb.DNI = personalAccount.DNI;
                accountFromDb.Address = personalAccount.Address;
                accountFromDb.Phone = personalAccount.Phone;
                accountFromDb.isActive = personalAccount.isActive;
            }
        }
    }
}
