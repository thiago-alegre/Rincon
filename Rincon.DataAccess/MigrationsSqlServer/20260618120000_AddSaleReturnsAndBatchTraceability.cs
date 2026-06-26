using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rincon.DataAccess.Data;

#nullable disable

namespace Rincon.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260618120000_AddSaleReturnsAndBatchTraceability")]
    public partial class AddSaleReturnsAndBatchTraceability : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "Sales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "Sales",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidedByUserId",
                table: "Sales",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "Sales",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SaleReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CashRegisterSessionId = table.Column<int>(type: "int", nullable: true),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsFullVoid = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleReturns_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SaleReturns_CashRegisterSessions_CashRegisterSessionId",
                        column: x => x.CashRegisterSessionId,
                        principalTable: "CashRegisterSessions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SaleReturns_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaleDetailBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleDetailId = table.Column<int>(type: "int", nullable: false),
                    ArticleBatchId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleDetailBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleDetailBatches_ArticleBatches_ArticleBatchId",
                        column: x => x.ArticleBatchId,
                        principalTable: "ArticleBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleDetailBatches_SaleDetails_SaleDetailId",
                        column: x => x.SaleDetailId,
                        principalTable: "SaleDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaleReturnDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleReturnId = table.Column<int>(type: "int", nullable: false),
                    SaleDetailId = table.Column<int>(type: "int", nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleReturnDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleReturnDetails_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleReturnDetails_SaleDetails_SaleDetailId",
                        column: x => x.SaleDetailId,
                        principalTable: "SaleDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleReturnDetails_SaleReturns_SaleReturnId",
                        column: x => x.SaleReturnId,
                        principalTable: "SaleReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaleReturnDetailBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleReturnDetailId = table.Column<int>(type: "int", nullable: false),
                    ArticleBatchId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleReturnDetailBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleReturnDetailBatches_ArticleBatches_ArticleBatchId",
                        column: x => x.ArticleBatchId,
                        principalTable: "ArticleBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleReturnDetailBatches_SaleReturnDetails_SaleReturnDetailId",
                        column: x => x.SaleReturnDetailId,
                        principalTable: "SaleReturnDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_VoidedByUserId",
                table: "Sales",
                column: "VoidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_CashRegisterSessionId",
                table: "SaleReturns",
                column: "CashRegisterSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_SaleId",
                table: "SaleReturns",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_UserId",
                table: "SaleReturns",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleDetailBatches_ArticleBatchId",
                table: "SaleDetailBatches",
                column: "ArticleBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleDetailBatches_SaleDetailId",
                table: "SaleDetailBatches",
                column: "SaleDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnDetails_ArticleId",
                table: "SaleReturnDetails",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnDetails_SaleDetailId",
                table: "SaleReturnDetails",
                column: "SaleDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnDetails_SaleReturnId",
                table: "SaleReturnDetails",
                column: "SaleReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnDetailBatches_ArticleBatchId",
                table: "SaleReturnDetailBatches",
                column: "ArticleBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnDetailBatches_SaleReturnDetailId",
                table: "SaleReturnDetailBatches",
                column: "SaleReturnDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_AspNetUsers_VoidedByUserId",
                table: "Sales",
                column: "VoidedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_AspNetUsers_VoidedByUserId",
                table: "Sales");

            migrationBuilder.DropTable(name: "SaleDetailBatches");
            migrationBuilder.DropTable(name: "SaleReturnDetailBatches");
            migrationBuilder.DropTable(name: "SaleReturnDetails");
            migrationBuilder.DropTable(name: "SaleReturns");

            migrationBuilder.DropIndex(
                name: "IX_Sales_VoidedByUserId",
                table: "Sales");

            migrationBuilder.DropColumn(name: "IsVoided", table: "Sales");
            migrationBuilder.DropColumn(name: "VoidedAt", table: "Sales");
            migrationBuilder.DropColumn(name: "VoidedByUserId", table: "Sales");
            migrationBuilder.DropColumn(name: "VoidReason", table: "Sales");
        }
    }
}
