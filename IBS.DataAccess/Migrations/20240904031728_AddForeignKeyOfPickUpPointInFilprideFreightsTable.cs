using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyOfPickUpPointInFilprideFreightsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pick_up_point",
                table: "filpride_freights");

            migrationBuilder.AddColumn<int>(
                name: "pick_up_point_id",
                table: "filpride_freights",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_freights_pick_up_point_id",
                table: "filpride_freights",
                column: "pick_up_point_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_freights_filpride_pick_up_points_pick_up_point_id",
                table: "filpride_freights",
                column: "pick_up_point_id",
                principalTable: "filpride_pick_up_points",
                principalColumn: "pick_up_point_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_freights_filpride_pick_up_points_pick_up_point_id",
                table: "filpride_freights");

            migrationBuilder.DropIndex(
                name: "ix_filpride_freights_pick_up_point_id",
                table: "filpride_freights");

            migrationBuilder.DropColumn(
                name: "pick_up_point_id",
                table: "filpride_freights");

            migrationBuilder.AddColumn<string>(
                name: "pick_up_point",
                table: "filpride_freights",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");
        }
    }
}
