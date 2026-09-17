using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemInAnERP.Migrations
{
    /// <inheritdoc />
    public partial class LinkDatabaseModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuotationId",
                table: "SalesOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "SalesOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Quotations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SalesOrderId",
                table: "ProductionOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "ProductionOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "ImportTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductionOrderId",
                table: "ExportTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "ExportTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_QuotationId",
                table: "SalesOrders",
                column: "QuotationId",
                unique: true,
                filter: "[QuotationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_UserId",
                table: "SalesOrders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_UserId",
                table: "Quotations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_SalesOrderId",
                table: "ProductionOrders",
                column: "SalesOrderId",
                unique: true,
                filter: "[SalesOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_UserId",
                table: "ProductionOrders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_UserId",
                table: "Invoices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportTransactions_UserId",
                table: "ImportTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportTransactions_ProductionOrderId",
                table: "ExportTransactions",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportTransactions_UserId",
                table: "ExportTransactions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExportTransactions_ProductionOrders_ProductionOrderId",
                table: "ExportTransactions",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExportTransactions_Users_UserId",
                table: "ExportTransactions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportTransactions_Users_UserId",
                table: "ImportTransactions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_UserId",
                table: "Invoices",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrders_SalesOrders_SalesOrderId",
                table: "ProductionOrders",
                column: "SalesOrderId",
                principalTable: "SalesOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrders_Users_UserId",
                table: "ProductionOrders",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotations_Users_UserId",
                table: "Quotations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Quotations_QuotationId",
                table: "SalesOrders",
                column: "QuotationId",
                principalTable: "Quotations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Users_UserId",
                table: "SalesOrders",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExportTransactions_ProductionOrders_ProductionOrderId",
                table: "ExportTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_ExportTransactions_Users_UserId",
                table: "ExportTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_ImportTransactions_Users_UserId",
                table: "ImportTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_UserId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrders_SalesOrders_SalesOrderId",
                table: "ProductionOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrders_Users_UserId",
                table: "ProductionOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotations_Users_UserId",
                table: "Quotations");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Quotations_QuotationId",
                table: "SalesOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Users_UserId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_QuotationId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_UserId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_Quotations_UserId",
                table: "Quotations");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_SalesOrderId",
                table: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_UserId",
                table: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_UserId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_ImportTransactions_UserId",
                table: "ImportTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ExportTransactions_ProductionOrderId",
                table: "ExportTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ExportTransactions_UserId",
                table: "ExportTransactions");

            migrationBuilder.DropColumn(
                name: "QuotationId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "SalesOrderId",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ImportTransactions");

            migrationBuilder.DropColumn(
                name: "ProductionOrderId",
                table: "ExportTransactions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ExportTransactions");
        }
    }
}
