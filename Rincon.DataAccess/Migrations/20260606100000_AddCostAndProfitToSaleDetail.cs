using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rincon.DataAccess.Data;

#nullable disable

namespace Rincon.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260606100000_AddCostAndProfitToSaleDetail")]
    public partial class AddCostAndProfitToSaleDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "SaleDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedProfit",
                table: "SaleDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(@"
                UPDATE sd
                SET
                    sd.UnitCost = ISNULL(a.Cost, 0),
                    sd.EstimatedProfit = CASE
                        WHEN sd.ArticleId IS NULL THEN 0
                        ELSE (sd.UnitPrice - ISNULL(a.Cost, 0)) * sd.Quantity
                    END
                FROM SaleDetails sd
                LEFT JOIN Articles a ON sd.ArticleId = a.Id
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedProfit",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "SaleDetails");
        }
    }
}
