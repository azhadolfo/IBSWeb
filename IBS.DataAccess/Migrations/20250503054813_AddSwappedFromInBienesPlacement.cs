using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSwappedFromInBienesPlacement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_swapped",
                table: "bienes_placements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "swapped_from_id",
                table: "bienes_placements",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_bienes_placements_swapped_from_id",
                table: "bienes_placements",
                column: "swapped_from_id");

            migrationBuilder.AddForeignKey(
                name: "fk_bienes_placements_bienes_placements_swapped_from_id",
                table: "bienes_placements",
                column: "swapped_from_id",
                principalTable: "bienes_placements",
                principalColumn: "placement_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bienes_placements_bienes_placements_swapped_from_id",
                table: "bienes_placements");

            migrationBuilder.DropIndex(
                name: "ix_bienes_placements_swapped_from_id",
                table: "bienes_placements");

            migrationBuilder.DropColumn(
                name: "is_swapped",
                table: "bienes_placements");

            migrationBuilder.DropColumn(
                name: "swapped_from_id",
                table: "bienes_placements");
        }
    }
}
