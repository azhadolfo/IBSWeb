using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateFieldTransactionNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "Inventories");

            migrationBuilder.AddColumn<string>(
                name: "LubePurchaseNo",
                table: "LubePurchaseHeaders",
                type: "varchar(25)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TransactionNo",
                table: "Inventories",
                type: "varchar(25)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FuelPurchaseNo",
                table: "FuelPurchase",
                type: "varchar(25)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LubePurchaseNo",
                table: "LubePurchaseHeaders");

            migrationBuilder.DropColumn(
                name: "TransactionNo",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "FuelPurchaseNo",
                table: "FuelPurchase");

            migrationBuilder.AddColumn<int>(
                name: "TransactionId",
                table: "Inventories",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
