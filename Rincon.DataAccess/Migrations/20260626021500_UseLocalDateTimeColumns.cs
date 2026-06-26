using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rincon.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UseLocalDateTimeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "AspNetUsers" ALTER COLUMN "Date" TYPE timestamp without time zone USING "Date"::timestamp without time zone;
                ALTER TABLE "Categories" ALTER COLUMN "Date" TYPE timestamp without time zone USING "Date"::timestamp without time zone;
                ALTER TABLE "PersonalAccounts" ALTER COLUMN "Date" TYPE timestamp without time zone USING "Date"::timestamp without time zone;
                ALTER TABLE "CashRegisterSessions" ALTER COLUMN "OpenedAt" TYPE timestamp without time zone USING "OpenedAt"::timestamp without time zone;
                ALTER TABLE "CashRegisterSessions" ALTER COLUMN "ClosedAt" TYPE timestamp without time zone USING "ClosedAt"::timestamp without time zone;
                ALTER TABLE "Articles" ALTER COLUMN "ExpirationDate" TYPE timestamp without time zone USING "ExpirationDate"::timestamp without time zone;
                ALTER TABLE "Articles" ALTER COLUMN "Date" TYPE timestamp without time zone USING "Date"::timestamp without time zone;
                ALTER TABLE "PersonalAccountPayments" ALTER COLUMN "Date" TYPE timestamp without time zone USING "Date"::timestamp without time zone;
                ALTER TABLE "Sales" ALTER COLUMN "Date" TYPE timestamp without time zone USING "Date"::timestamp without time zone;
                ALTER TABLE "Sales" ALTER COLUMN "PersonalAccountSettledAt" TYPE timestamp without time zone USING "PersonalAccountSettledAt"::timestamp without time zone;
                ALTER TABLE "Sales" ALTER COLUMN "VoidedAt" TYPE timestamp without time zone USING "VoidedAt"::timestamp without time zone;
                ALTER TABLE "ArticleBatches" ALTER COLUMN "ExpirationDate" TYPE timestamp without time zone USING "ExpirationDate"::timestamp without time zone;
                ALTER TABLE "ArticleBatches" ALTER COLUMN "PurchaseDate" TYPE timestamp without time zone USING "PurchaseDate"::timestamp without time zone;
                ALTER TABLE "ArticleBatches" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone USING "CreatedAt"::timestamp without time zone;
                ALTER TABLE "SaleReturns" ALTER COLUMN "Date" TYPE timestamp without time zone USING "Date"::timestamp without time zone;
                ALTER TABLE "SaleExchanges" ALTER COLUMN "Date" TYPE timestamp without time zone USING "Date"::timestamp without time zone;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "AspNetUsers" ALTER COLUMN "Date" TYPE timestamp with time zone USING "Date"::timestamp with time zone;
                ALTER TABLE "Categories" ALTER COLUMN "Date" TYPE timestamp with time zone USING "Date"::timestamp with time zone;
                ALTER TABLE "PersonalAccounts" ALTER COLUMN "Date" TYPE timestamp with time zone USING "Date"::timestamp with time zone;
                ALTER TABLE "CashRegisterSessions" ALTER COLUMN "OpenedAt" TYPE timestamp with time zone USING "OpenedAt"::timestamp with time zone;
                ALTER TABLE "CashRegisterSessions" ALTER COLUMN "ClosedAt" TYPE timestamp with time zone USING "ClosedAt"::timestamp with time zone;
                ALTER TABLE "Articles" ALTER COLUMN "ExpirationDate" TYPE timestamp with time zone USING "ExpirationDate"::timestamp with time zone;
                ALTER TABLE "Articles" ALTER COLUMN "Date" TYPE timestamp with time zone USING "Date"::timestamp with time zone;
                ALTER TABLE "PersonalAccountPayments" ALTER COLUMN "Date" TYPE timestamp with time zone USING "Date"::timestamp with time zone;
                ALTER TABLE "Sales" ALTER COLUMN "Date" TYPE timestamp with time zone USING "Date"::timestamp with time zone;
                ALTER TABLE "Sales" ALTER COLUMN "PersonalAccountSettledAt" TYPE timestamp with time zone USING "PersonalAccountSettledAt"::timestamp with time zone;
                ALTER TABLE "Sales" ALTER COLUMN "VoidedAt" TYPE timestamp with time zone USING "VoidedAt"::timestamp with time zone;
                ALTER TABLE "ArticleBatches" ALTER COLUMN "ExpirationDate" TYPE timestamp with time zone USING "ExpirationDate"::timestamp with time zone;
                ALTER TABLE "ArticleBatches" ALTER COLUMN "PurchaseDate" TYPE timestamp with time zone USING "PurchaseDate"::timestamp with time zone;
                ALTER TABLE "ArticleBatches" ALTER COLUMN "CreatedAt" TYPE timestamp with time zone USING "CreatedAt"::timestamp with time zone;
                ALTER TABLE "SaleReturns" ALTER COLUMN "Date" TYPE timestamp with time zone USING "Date"::timestamp with time zone;
                ALTER TABLE "SaleExchanges" ALTER COLUMN "Date" TYPE timestamp with time zone USING "Date"::timestamp with time zone;
                """);
        }
    }
}
