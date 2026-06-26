using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rincon.DataAccess.Data;

#nullable disable

namespace Rincon.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260620130000_AddReturnsToStockToSaleReturnDetail")]
    public partial class AddReturnsToStockToSaleReturnDetail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReturnsToStock",
                table: "SaleReturnDetails",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnsToStock",
                table: "SaleReturnDetails");
        }
    }
}
