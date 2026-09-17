using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemInAnERP.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quotations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuoteDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidityDays = table.Column<int>(type: "int", nullable: false),
                    DeliveryDays = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductDimensions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    SoCon = table.Column<int>(type: "int", nullable: false),
                    BuHao = table.Column<int>(type: "int", nullable: false),
                    PaperType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaperGsm = table.Column<double>(type: "float", nullable: false),
                    PaperPricePerTon = table.Column<double>(type: "float", nullable: false),
                    PrintLength = table.Column<double>(type: "float", nullable: false),
                    PrintWidth = table.Column<double>(type: "float", nullable: false),
                    ColorCount = table.Column<int>(type: "int", nullable: false),
                    IsLargeMachine = table.Column<bool>(type: "bit", nullable: false),
                    LaminationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LaminationSides = table.Column<int>(type: "int", nullable: false),
                    LaminationPrice = table.Column<double>(type: "float", nullable: false),
                    DieCutMoldPrice = table.Column<double>(type: "float", nullable: false),
                    StringPricePerItem = table.Column<double>(type: "float", nullable: false),
                    ButtonPricePerItem = table.Column<double>(type: "float", nullable: false),
                    BoxPrice = table.Column<double>(type: "float", nullable: false),
                    DeliveryFee = table.Column<double>(type: "float", nullable: false),
                    ProfitMargin = table.Column<double>(type: "float", nullable: false),
                    TotalProductionCost = table.Column<double>(type: "float", nullable: false),
                    QuotedUnitPrice = table.Column<double>(type: "float", nullable: false),
                    TotalOrderValue = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuoteExtraCosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<double>(type: "float", nullable: false),
                    UnitPrice = table.Column<double>(type: "float", nullable: false),
                    TotalPrice = table.Column<double>(type: "float", nullable: false),
                    QuotationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteExtraCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteExtraCosts_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "Quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteExtraCosts_QuotationId",
                table: "QuoteExtraCosts",
                column: "QuotationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuoteExtraCosts");

            migrationBuilder.DropTable(
                name: "Quotations");
        }
    }
}
