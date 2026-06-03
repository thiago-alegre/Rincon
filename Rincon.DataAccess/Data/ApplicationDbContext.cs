using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rincon.Models;

namespace Rincon.DataAccess.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleDetail> SaleDetails { get; set; }
        public DbSet<CashRegisterSession> CashRegisterSessions { get; set; }
        public DbSet<PersonalAccount> PersonalAccounts { get; set; }
        public DbSet<PersonalAccountPayment> PersonalAccountPayments { get; set; }
    }
}
