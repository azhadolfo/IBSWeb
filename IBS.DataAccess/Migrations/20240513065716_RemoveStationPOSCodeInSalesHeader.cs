using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStationPOSCodeInSalesHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesHeaders_StationPosCode",
                table: "SalesHeaders");

            migrationBuilder.DropColumn(
                name: "StationPosCode",
                table: "SalesHeaders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StationPosCode",
                table: "SalesHeaders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SalesHeaders_StationPosCode",
                table: "SalesHeaders",
                column: "StationPosCode");
        }
    }
}
