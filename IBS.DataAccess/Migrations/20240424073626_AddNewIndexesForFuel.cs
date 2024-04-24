using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNewIndexesForFuel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SalesHeaders_Date",
                table: "SalesHeaders",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_SalesHeaders_Shift",
                table: "SalesHeaders",
                column: "Shift");

            migrationBuilder.CreateIndex(
                name: "IX_SalesHeaders_StationPosCode",
                table: "SalesHeaders",
                column: "StationPosCode");

            migrationBuilder.CreateIndex(
                name: "IX_Fuels_IsProcessed",
                table: "Fuels",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_Fuels_ItemCode",
                table: "Fuels",
                column: "ItemCode");

            migrationBuilder.CreateIndex(
                name: "IX_Fuels_Particulars",
                table: "Fuels",
                column: "Particulars");

            migrationBuilder.CreateIndex(
                name: "IX_Fuels_Shift",
                table: "Fuels",
                column: "Shift");

            migrationBuilder.CreateIndex(
                name: "IX_Fuels_xPUMP",
                table: "Fuels",
                column: "xPUMP");

            migrationBuilder.CreateIndex(
                name: "IX_Fuels_xSITECODE",
                table: "Fuels",
                column: "xSITECODE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesHeaders_Date",
                table: "SalesHeaders");

            migrationBuilder.DropIndex(
                name: "IX_SalesHeaders_Shift",
                table: "SalesHeaders");

            migrationBuilder.DropIndex(
                name: "IX_SalesHeaders_StationPosCode",
                table: "SalesHeaders");

            migrationBuilder.DropIndex(
                name: "IX_Fuels_IsProcessed",
                table: "Fuels");

            migrationBuilder.DropIndex(
                name: "IX_Fuels_ItemCode",
                table: "Fuels");

            migrationBuilder.DropIndex(
                name: "IX_Fuels_Particulars",
                table: "Fuels");

            migrationBuilder.DropIndex(
                name: "IX_Fuels_Shift",
                table: "Fuels");

            migrationBuilder.DropIndex(
                name: "IX_Fuels_xPUMP",
                table: "Fuels");

            migrationBuilder.DropIndex(
                name: "IX_Fuels_xSITECODE",
                table: "Fuels");
        }
    }
}
