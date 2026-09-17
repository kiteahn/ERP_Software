using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemInAnERP.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExportTransactionAndAddProductionImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExportTransactions_Materials_MaterialId",
                table: "ExportTransactions");

            migrationBuilder.AlterColumn<int>(
                name: "MaterialId",
                table: "ExportTransactions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_ExportTransactions_Materials_MaterialId",
                table: "ExportTransactions",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExportTransactions_Materials_MaterialId",
                table: "ExportTransactions");

            migrationBuilder.AlterColumn<int>(
                name: "MaterialId",
                table: "ExportTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExportTransactions_Materials_MaterialId",
                table: "ExportTransactions",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
