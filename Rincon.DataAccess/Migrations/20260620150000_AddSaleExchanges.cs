using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rincon.DataAccess.Data;

#nullable disable

namespace Rincon.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260620150000_AddSaleExchanges")]
    public partial class AddSaleExchanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SaleExchanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleId = table.Column<int>(type: "int", nullable: false),
                    SaleDetailId = table.Column<int>(type: "int", nullable: false),
                    OriginalArticleId = table.Column<int>(type: "int", nullable: true),
                    ReplacementArticleId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    ReplacementUnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedLoss = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CashRegisterSessionId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleExchanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleExchanges_Articles_OriginalArticleId",
                        column: x => x.OriginalArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleExchanges_Articles_ReplacementArticleId",
                        column: x => x.ReplacementArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleExchanges_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SaleExchanges_CashRegisterSessions_CashRegisterSessionId",
                        column: x => x.CashRegisterSessionId,
                        principalTable: "CashRegisterSessions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SaleExchanges_SaleDetails_SaleDetailId",
                        column: x => x.SaleDetailId,
                        principalTable: "SaleDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleExchanges_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaleExchangeBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleExchangeId = table.Column<int>(type: "int", nullable: false),
                    ArticleBatchId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleExchangeBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleExchangeBatches_ArticleBatches_ArticleBatchId",
                        column: x => x.ArticleBatchId,
                        principalTable: "ArticleBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleExchangeBatches_SaleExchanges_SaleExchangeId",
                        column: x => x.SaleExchangeId,
                        principalTable: "SaleExchanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaleExchanges_CashRegisterSessionId",
                table: "SaleExchanges",
                column: "CashRegisterSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleExchanges_OriginalArticleId",
                table: "SaleExchanges",
                column: "OriginalArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleExchanges_ReplacementArticleId",
                table: "SaleExchanges",
                column: "ReplacementArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleExchanges_SaleDetailId",
                table: "SaleExchanges",
                column: "SaleDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleExchanges_SaleId",
                table: "SaleExchanges",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleExchanges_UserId",
                table: "SaleExchanges",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleExchangeBatches_ArticleBatchId",
                table: "SaleExchangeBatches",
                column: "ArticleBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleExchangeBatches_SaleExchangeId",
                table: "SaleExchangeBatches",
                column: "SaleExchangeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SaleExchangeBatches");
            migrationBuilder.DropTable(name: "SaleExchanges");
        }
    }
}
