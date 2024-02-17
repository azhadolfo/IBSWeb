using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RechangeTheIndexName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_SalesHeader_Cashier",
                table: "SalesHeaders",
                newName: "IX_SalesHeaders_Cashier");

            migrationBuilder.RenameIndex(
                name: "IX_SafeDrop_xONAME",
                table: "SafeDrops",
                newName: "IX_SafeDrops_xONAME");

            migrationBuilder.RenameIndex(
                name: "IX_SafeDrop_INV_DATE",
                table: "SafeDrops",
                newName: "IX_SafeDrops_INV_DATE");

            migrationBuilder.RenameIndex(
                name: "IX_Lube_INV_DATE",
                table: "Lubes",
                newName: "IX_Lubes_INV_DATE");

            migrationBuilder.RenameIndex(
                name: "IX_Lube_Cashier",
                table: "Lubes",
                newName: "IX_Lubes_Cashier");

            migrationBuilder.RenameIndex(
                name: "IX_Fuel_xONAME",
                table: "Fuels",
                newName: "IX_Fuels_xONAME");

            migrationBuilder.RenameIndex(
                name: "IX_Fuel_INV_DATE",
                table: "Fuels",
                newName: "IX_Fuels_INV_DATE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_SalesHeaders_Cashier",
                table: "SalesHeaders",
                newName: "IX_SalesHeader_Cashier");

            migrationBuilder.RenameIndex(
                name: "IX_SafeDrops_xONAME",
                table: "SafeDrops",
                newName: "IX_SafeDrop_xONAME");

            migrationBuilder.RenameIndex(
                name: "IX_SafeDrops_INV_DATE",
                table: "SafeDrops",
                newName: "IX_SafeDrop_INV_DATE");

            migrationBuilder.RenameIndex(
                name: "IX_Lubes_INV_DATE",
                table: "Lubes",
                newName: "IX_Lube_INV_DATE");

            migrationBuilder.RenameIndex(
                name: "IX_Lubes_Cashier",
                table: "Lubes",
                newName: "IX_Lube_Cashier");

            migrationBuilder.RenameIndex(
                name: "IX_Fuels_xONAME",
                table: "Fuels",
                newName: "IX_Fuel_xONAME");

            migrationBuilder.RenameIndex(
                name: "IX_Fuels_INV_DATE",
                table: "Fuels",
                newName: "IX_Fuel_INV_DATE");
        }
    }
}
