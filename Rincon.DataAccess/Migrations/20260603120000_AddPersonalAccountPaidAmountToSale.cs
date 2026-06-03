using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rincon.DataAccess.Data;

#nullable disable

namespace Rincon.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260603120000_AddPersonalAccountPaidAmountToSale")]
    public partial class AddPersonalAccountPaidAmountToSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PersonalAccountPaidAmount",
                table: "Sales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(@"
                UPDATE Sales
                SET PersonalAccountPaidAmount = Total
                WHERE PaymentMethod = 3 AND IsPersonalAccountSettled = 1
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonalAccountPaidAmount",
                table: "Sales");
        }
    }
}
