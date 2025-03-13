using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierIdAndCompanyInPickupPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_pick_up_points",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_pick_up_points_company",
                table: "filpride_pick_up_points",
                column: "company");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_pick_up_points_filpride_suppliers_supplier_id",
                table: "filpride_pick_up_points");

            migrationBuilder.DropIndex(
                name: "ix_filpride_pick_up_points_company",
                table: "filpride_pick_up_points");

            migrationBuilder.DropIndex(
                name: "ix_filpride_pick_up_points_supplier_id",
                table: "filpride_pick_up_points");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_pick_up_points");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "filpride_pick_up_points");
        }
    }
}
