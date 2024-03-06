using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateForeignKeyInInventoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StationId",
                table: "Inventories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_StationId",
                table: "Inventories",
                column: "StationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Stations_StationId",
                table: "Inventories",
                column: "StationId",
                principalTable: "Stations",
                principalColumn: "StationId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Stations_StationId",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_StationId",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "StationId",
                table: "Inventories");
        }
    }
}
