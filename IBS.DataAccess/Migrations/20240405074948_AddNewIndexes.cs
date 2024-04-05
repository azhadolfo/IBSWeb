using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNewIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StationCode",
                table: "Inventories",
                type: "varchar(10)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(10)");

            migrationBuilder.CreateIndex(
                name: "IX_SalesHeaders_StationPosCode",
                table: "SalesHeaders",
                column: "StationPosCode");

            migrationBuilder.CreateIndex(
                name: "IX_LubePurchaseHeaders_StationCode",
                table: "LubePurchaseHeaders",
                column: "StationCode");

            migrationBuilder.CreateIndex(
                name: "IX_LubePurchaseDetails_ProductCode",
                table: "LubePurchaseDetails",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralLedgers_CustomerCode",
                table: "GeneralLedgers",
                column: "CustomerCode");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralLedgers_StationCode",
                table: "GeneralLedgers",
                column: "StationCode");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralLedgers_SupplierCode",
                table: "GeneralLedgers",
                column: "SupplierCode");

            migrationBuilder.CreateIndex(
                name: "IX_FuelPurchase_ProductCode",
                table: "FuelPurchase",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_FuelPurchase_StationCode",
                table: "FuelPurchase",
                column: "StationCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesHeaders_StationPosCode",
                table: "SalesHeaders");

            migrationBuilder.DropIndex(
                name: "IX_LubePurchaseHeaders_StationCode",
                table: "LubePurchaseHeaders");

            migrationBuilder.DropIndex(
                name: "IX_LubePurchaseDetails_ProductCode",
                table: "LubePurchaseDetails");

            migrationBuilder.DropIndex(
                name: "IX_GeneralLedgers_CustomerCode",
                table: "GeneralLedgers");

            migrationBuilder.DropIndex(
                name: "IX_GeneralLedgers_StationCode",
                table: "GeneralLedgers");

            migrationBuilder.DropIndex(
                name: "IX_GeneralLedgers_SupplierCode",
                table: "GeneralLedgers");

            migrationBuilder.DropIndex(
                name: "IX_FuelPurchase_ProductCode",
                table: "FuelPurchase");

            migrationBuilder.DropIndex(
                name: "IX_FuelPurchase_StationCode",
                table: "FuelPurchase");

            migrationBuilder.AlterColumn<string>(
                name: "StationCode",
                table: "Inventories",
                type: "varchar(10)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldNullable: true);
        }
    }
}
