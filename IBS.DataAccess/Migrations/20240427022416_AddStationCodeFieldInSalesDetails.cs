using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddStationCodeFieldInSalesDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StationCode",
                table: "SalesDetails",
                type: "varchar(3)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDetails_StationCode",
                table: "SalesDetails",
                column: "StationCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesDetails_StationCode",
                table: "SalesDetails");

            migrationBuilder.DropColumn(
                name: "StationCode",
                table: "SalesDetails");
        }
    }
}
