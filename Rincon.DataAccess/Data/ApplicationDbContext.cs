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
        public DbSet<ArticleBatch> ArticleBatches { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleDetail> SaleDetails { get; set; }
        public DbSet<SaleDetailBatch> SaleDetailBatches { get; set; }
        public DbSet<SaleReturn> SaleReturns { get; set; }
        public DbSet<SaleReturnDetail> SaleReturnDetails { get; set; }
        public DbSet<SaleReturnDetailBatch> SaleReturnDetailBatches { get; set; }
        public DbSet<SaleExchange> SaleExchanges { get; set; }
        public DbSet<SaleExchangeBatch> SaleExchangeBatches { get; set; }
        public DbSet<CashRegisterSession> CashRegisterSessions { get; set; }
        public DbSet<PersonalAccount> PersonalAccounts { get; set; }
        public DbSet<PersonalAccountPayment> PersonalAccountPayments { get; set; }
    }
}
