using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNewArraysToSalesHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "Customers",
                table: "SalesHeaders",
                type: "varchar[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<decimal[]>(
                name: "POSalesAmount",
                table: "SalesHeaders",
                type: "numeric(18,2)[]",
                nullable: false,
                defaultValue: new decimal[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Customers",
                table: "SalesHeaders");

            migrationBuilder.DropColumn(
                name: "POSalesAmount",
                table: "SalesHeaders");
        }
    }
}
