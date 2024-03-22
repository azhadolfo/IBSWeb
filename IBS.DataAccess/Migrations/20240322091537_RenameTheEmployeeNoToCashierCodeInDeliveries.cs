using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameTheEmployeeNoToCashierCodeInDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmployeeNo",
                table: "LubePurchaseHeaders",
                newName: "CashierCode");

            migrationBuilder.RenameColumn(
                name: "EmployeeNo",
                table: "FuelPurchase",
                newName: "CashierCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CashierCode",
                table: "LubePurchaseHeaders",
                newName: "EmployeeNo");

            migrationBuilder.RenameColumn(
                name: "CashierCode",
                table: "FuelPurchase",
                newName: "EmployeeNo");
        }
    }
}
