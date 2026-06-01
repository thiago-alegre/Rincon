using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rincon.DataAccess.Data;

#nullable disable

namespace Rincon.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260601120000_AddPersonalAccounts")]
    public partial class AddPersonalAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonalAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DNI = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalAccounts", x => x.Id);
                });

            migrationBuilder.AddColumn<bool>(
                name: "IsPersonalAccountSettled",
                table: "Sales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PersonalAccountId",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PersonalAccountSettledAt",
                table: "Sales",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_PersonalAccountId",
                table: "Sales",
                column: "PersonalAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_PersonalAccounts_PersonalAccountId",
                table: "Sales",
                column: "PersonalAccountId",
                principalTable: "PersonalAccounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_PersonalAccounts_PersonalAccountId",
                table: "Sales");

            migrationBuilder.DropTable(
                name: "PersonalAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Sales_PersonalAccountId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "IsPersonalAccountSettled",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PersonalAccountId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PersonalAccountSettledAt",
                table: "Sales");
        }
    }
}
