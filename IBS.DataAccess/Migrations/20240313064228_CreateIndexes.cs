using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly>(
                name: "Date",
                table: "Inventories",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SupplierCode",
                table: "Suppliers",
                column: "SupplierCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SupplierName",
                table: "Suppliers",
                column: "SupplierName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stations_PosCode",
                table: "Stations",
                column: "PosCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stations_StationCode",
                table: "Stations",
                column: "StationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stations_StationName",
                table: "Stations",
                column: "StationName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LubePurchaseHeaders_LubePurchaseHeaderNo",
                table: "LubePurchaseHeaders",
                column: "LubePurchaseHeaderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LubePurchaseDetails_LubePurchaseHeaderNo",
                table: "LubePurchaseDetails",
                column: "LubePurchaseHeaderNo");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ProductCode",
                table: "Inventories",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_StationCode",
                table: "Inventories",
                column: "StationCode");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_TransactionNo",
                table: "Inventories",
                column: "TransactionNo");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralLedgers_AccountNumber",
                table: "GeneralLedgers",
                column: "AccountNumber");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralLedgers_AccountTitle",
                table: "GeneralLedgers",
                column: "AccountTitle");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralLedgers_JournalReference",
                table: "GeneralLedgers",
                column: "JournalReference");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralLedgers_ProductCode",
                table: "GeneralLedgers",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralLedgers_Reference",
                table: "GeneralLedgers",
                column: "Reference");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralLedgers_TransactionDate",
                table: "GeneralLedgers",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_FuelPurchase_FuelPurchaseNo",
                table: "FuelPurchase",
                column: "FuelPurchaseNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suppliers_SupplierCode",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_SupplierName",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Stations_PosCode",
                table: "Stations");

            migrationBuilder.DropIndex(
                name: "IX_Stations_StationCode",
                table: "Stations");

            migrationBuilder.DropIndex(
                name: "IX_Stations_StationName",
                table: "Stations");

            migrationBuilder.DropIndex(
                name: "IX_LubePurchaseHeaders_LubePurchaseHeaderNo",
                table: "LubePurchaseHeaders");

            migrationBuilder.DropIndex(
                name: "IX_LubePurchaseDetails_LubePurchaseHeaderNo",
                table: "LubePurchaseDetails");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_ProductCode",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_StationCode",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_TransactionNo",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_GeneralLedgers_AccountNumber",
                table: "GeneralLedgers");

            migrationBuilder.DropIndex(
                name: "IX_GeneralLedgers_AccountTitle",
                table: "GeneralLedgers");

            migrationBuilder.DropIndex(
                name: "IX_GeneralLedgers_JournalReference",
                table: "GeneralLedgers");

            migrationBuilder.DropIndex(
                name: "IX_GeneralLedgers_ProductCode",
                table: "GeneralLedgers");

            migrationBuilder.DropIndex(
                name: "IX_GeneralLedgers_Reference",
                table: "GeneralLedgers");

            migrationBuilder.DropIndex(
                name: "IX_GeneralLedgers_TransactionDate",
                table: "GeneralLedgers");

            migrationBuilder.DropIndex(
                name: "IX_FuelPurchase_FuelPurchaseNo",
                table: "FuelPurchase");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "Date",
                table: "Inventories",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }
    }
}
