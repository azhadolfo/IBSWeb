using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTheIsUniqueNoForPurchasesMobility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_mobility_fuel_purchase_fuel_purchase_no",
                table: "mobility_fuel_purchase");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuel_purchase_fuel_purchase_no",
                table: "mobility_fuel_purchase",
                column: "fuel_purchase_no");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_mobility_fuel_purchase_fuel_purchase_no",
                table: "mobility_fuel_purchase");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuel_purchase_fuel_purchase_no",
                table: "mobility_fuel_purchase",
                column: "fuel_purchase_no",
                unique: true);
        }
    }
}
