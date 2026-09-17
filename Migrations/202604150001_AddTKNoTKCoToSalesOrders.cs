using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemInAnERP.Migrations
{
    /// <inheritdoc />
    public partial class AddTKNoTKCoToSalesOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TK_No",
                table: "SalesOrders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TK_Co",
                table: "SalesOrders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TK_No",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "TK_Co",
                table: "SalesOrders");
        }
    }
}