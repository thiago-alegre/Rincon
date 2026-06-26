using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rincon.DataAccess.Data;

#nullable disable

namespace Rincon.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260528120000_AddCashRegisterSessions")]
    public partial class AddCashRegisterSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashRegisterSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpeningAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CountedCashAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExpectedCashAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Difference = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashRegisterSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashRegisterSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "CashRegisterSessionId",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CashRegisterSessionId",
                table: "Sales",
                column: "CashRegisterSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisterSessions_UserId",
                table: "CashRegisterSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_CashRegisterSessions_CashRegisterSessionId",
                table: "Sales",
                column: "CashRegisterSessionId",
                principalTable: "CashRegisterSessions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_CashRegisterSessions_CashRegisterSessionId",
                table: "Sales");

            migrationBuilder.DropTable(
                name: "CashRegisterSessions");

            migrationBuilder.DropIndex(
                name: "IX_Sales_CashRegisterSessionId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CashRegisterSessionId",
                table: "Sales");
        }
    }
}
