using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFieldsInLubesPurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "LubePurchaseDetails",
                newName: "CostPerPiece");

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                table: "LubePurchaseHeaders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatableSales",
                table: "LubePurchaseHeaders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CostPerCase",
                table: "LubePurchaseDetails",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "LubePurchaseHeaders");

            migrationBuilder.DropColumn(
                name: "VatableSales",
                table: "LubePurchaseHeaders");

            migrationBuilder.DropColumn(
                name: "CostPerCase",
                table: "LubePurchaseDetails");

            migrationBuilder.RenameColumn(
                name: "CostPerPiece",
                table: "LubePurchaseDetails",
                newName: "UnitPrice");
        }
    }
}
