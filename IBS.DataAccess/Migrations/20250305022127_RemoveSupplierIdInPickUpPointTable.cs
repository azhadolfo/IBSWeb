using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSupplierIdInPickUpPointTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_pick_up_points_filpride_suppliers_supplier_id",
                table: "filpride_pick_up_points");

            migrationBuilder.DropIndex(
                name: "ix_filpride_pick_up_points_supplier_id",
                table: "filpride_pick_up_points");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "filpride_pick_up_points");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "supplier_id",
                table: "filpride_pick_up_points",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_pick_up_points_supplier_id",
                table: "filpride_pick_up_points",
                column: "supplier_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_pick_up_points_filpride_suppliers_supplier_id",
                table: "filpride_pick_up_points",
                column: "supplier_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
