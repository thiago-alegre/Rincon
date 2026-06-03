using Rincon.DataAccess.Data.Repository.IRepository;
using Rincon.Models;

namespace Rincon.DataAccess.Data.Repository
{
    public class PersonalAccountPaymentRepository : Repository<PersonalAccountPayment>, IPersonalAccountPaymentRepository
    {
        private readonly ApplicationDbContext _db;

        public PersonalAccountPaymentRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(PersonalAccountPayment personalAccountPayment)
        {
            _db.PersonalAccountPayments.Update(personalAccountPayment);
        }
    }
}
