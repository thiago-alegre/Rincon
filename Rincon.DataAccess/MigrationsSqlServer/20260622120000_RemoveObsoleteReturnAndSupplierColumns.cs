using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rincon.DataAccess.Migrations
{
    public partial class RemoveObsoleteReturnAndSupplierColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplierName",
                table: "ArticleBatches");

            migrationBuilder.DropColumn(
                name: "ReturnsToStock",
                table: "SaleReturnDetails");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SupplierName",
                table: "ArticleBatches",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReturnsToStock",
                table: "SaleReturnDetails",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }
    }
}
