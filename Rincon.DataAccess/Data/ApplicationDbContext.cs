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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var property in builder.Model.GetEntityTypes()
                .SelectMany(e => e.GetProperties())
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
            {
                property.SetColumnType("timestamp without time zone");
            }

            builder.Entity<Article>(entity =>
            {
                entity.Property(e => e.Price).HasPrecision(18, 2);
                entity.Property(e => e.Cost).HasPrecision(18, 2);
                entity.Property(e => e.Stock).HasPrecision(18, 3);
                entity.Property(e => e.StockMin).HasPrecision(18, 3);
            });

            builder.Entity<ArticleBatch>(entity =>
            {
                entity.Property(e => e.Quantity).HasPrecision(18, 3);
                entity.Property(e => e.InitialQuantity).HasPrecision(18, 3);
                entity.Property(e => e.Cost).HasPrecision(18, 2);
            });

            builder.Entity<CashRegisterSession>(entity =>
            {
                entity.Property(e => e.OpeningAmount).HasPrecision(18, 2);
                entity.Property(e => e.ExpectedCashAmount).HasPrecision(18, 2);
                entity.Property(e => e.CountedCashAmount).HasPrecision(18, 2);
                entity.Property(e => e.Difference).HasPrecision(18, 2);
            });

            builder.Entity<PersonalAccountPayment>()
                .Property(e => e.Amount)
                .HasPrecision(18, 2);

            builder.Entity<Sale>(entity =>
            {
                entity.Property(e => e.Total).HasPrecision(18, 2);
                entity.Property(e => e.AmountReceived).HasPrecision(18, 2);
                entity.Property(e => e.Change).HasPrecision(18, 2);
                entity.Property(e => e.PersonalAccountPaidAmount).HasPrecision(18, 2);
            });

            builder.Entity<SaleDetail>(entity =>
            {
                entity.Property(e => e.Quantity).HasPrecision(18, 3);
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                entity.Property(e => e.Subtotal).HasPrecision(18, 2);
                entity.Property(e => e.UnitCost).HasPrecision(18, 2);
                entity.Property(e => e.EstimatedProfit).HasPrecision(18, 2);
            });

            builder.Entity<SaleDetailBatch>(entity =>
            {
                entity.Property(e => e.Quantity).HasPrecision(18, 3);
                entity.Property(e => e.UnitCost).HasPrecision(18, 2);
            });

            builder.Entity<SaleReturn>()
                .Property(e => e.Total)
                .HasPrecision(18, 2);

            builder.Entity<SaleReturnDetail>(entity =>
            {
                entity.Property(e => e.Quantity).HasPrecision(18, 3);
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                entity.Property(e => e.Subtotal).HasPrecision(18, 2);
            });

            builder.Entity<SaleReturnDetailBatch>()
                .Property(e => e.Quantity)
                .HasPrecision(18, 3);

            builder.Entity<SaleExchange>(entity =>
            {
                entity.Property(e => e.Quantity).HasPrecision(18, 3);
                entity.Property(e => e.ReplacementUnitCost).HasPrecision(18, 2);
                entity.Property(e => e.EstimatedLoss).HasPrecision(18, 2);
            });

            builder.Entity<SaleExchangeBatch>(entity =>
            {
                entity.Property(e => e.Quantity).HasPrecision(18, 3);
                entity.Property(e => e.UnitCost).HasPrecision(18, 2);
            });
        }
    }
}
