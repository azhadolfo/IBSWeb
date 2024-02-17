using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateIndexForSomeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_SafeDrops_xONAME",
                table: "SafeDrops",
                newName: "IX_SafeDrop_xONAME");

            migrationBuilder.RenameIndex(
                name: "IX_Lubes_Cashier",
                table: "Lubes",
                newName: "IX_Lube_Cashier");

            migrationBuilder.RenameIndex(
                name: "IX_Fuels_xONAME",
                table: "Fuels",
                newName: "IX_Fuel_xONAME");

            migrationBuilder.CreateIndex(
                name: "IX_SalesHeader_Cashier",
                table: "SalesHeaders",
                column: "Cashier");

            migrationBuilder.CreateIndex(
                name: "IX_SalesHeaders_SalesHeaderId",
                table: "SalesHeaders",
                column: "SalesHeaderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesHeaders_SalesNo",
                table: "SalesHeaders",
                column: "SalesNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesDetails_SalesNo",
                table: "SalesDetails",
                column: "SalesNo");

            migrationBuilder.CreateIndex(
                name: "IX_SafeDrop_INV_DATE",
                table: "SafeDrops",
                column: "INV_DATE");

            migrationBuilder.CreateIndex(
                name: "IX_Lube_INV_DATE",
                table: "Lubes",
                column: "INV_DATE");

            migrationBuilder.CreateIndex(
                name: "IX_Fuel_INV_DATE",
                table: "Fuels",
                column: "INV_DATE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesHeader_Cashier",
                table: "SalesHeaders");

            migrationBuilder.DropIndex(
                name: "IX_SalesHeaders_SalesHeaderId",
                table: "SalesHeaders");

            migrationBuilder.DropIndex(
                name: "IX_SalesHeaders_SalesNo",
                table: "SalesHeaders");

            migrationBuilder.DropIndex(
                name: "IX_SalesDetails_SalesNo",
                table: "SalesDetails");

            migrationBuilder.DropIndex(
                name: "IX_SafeDrop_INV_DATE",
                table: "SafeDrops");

            migrationBuilder.DropIndex(
                name: "IX_Lube_INV_DATE",
                table: "Lubes");

            migrationBuilder.DropIndex(
                name: "IX_Fuel_INV_DATE",
                table: "Fuels");

            migrationBuilder.RenameIndex(
                name: "IX_SafeDrop_xONAME",
                table: "SafeDrops",
                newName: "IX_SafeDrops_xONAME");

            migrationBuilder.RenameIndex(
                name: "IX_Lube_Cashier",
                table: "Lubes",
                newName: "IX_Lubes_Cashier");

            migrationBuilder.RenameIndex(
                name: "IX_Fuel_xONAME",
                table: "Fuels",
                newName: "IX_Fuels_xONAME");
        }
    }
}
