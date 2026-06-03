using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rincon.DataAccess.Data;

#nullable disable

namespace Rincon.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260601130000_AddPersonalAccountPayments")]
    public partial class AddPersonalAccountPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonalAccountPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PersonalAccountId = table.Column<int>(type: "int", nullable: false),
                    CashRegisterSessionId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalAccountPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalAccountPayments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonalAccountPayments_CashRegisterSessions_CashRegisterSessionId",
                        column: x => x.CashRegisterSessionId,
                        principalTable: "CashRegisterSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonalAccountPayments_PersonalAccounts_PersonalAccountId",
                        column: x => x.PersonalAccountId,
                        principalTable: "PersonalAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalAccountPayments_CashRegisterSessionId",
                table: "PersonalAccountPayments",
                column: "CashRegisterSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalAccountPayments_PersonalAccountId",
                table: "PersonalAccountPayments",
                column: "PersonalAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalAccountPayments_UserId",
                table: "PersonalAccountPayments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonalAccountPayments");
        }
    }
}
