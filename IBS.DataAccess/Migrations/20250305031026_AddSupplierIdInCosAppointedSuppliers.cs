using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierIdInCosAppointedSuppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "supplier_id",
                table: "filpride_cos_appointed_suppliers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_cos_appointed_suppliers_supplier_id",
                table: "filpride_cos_appointed_suppliers",
                column: "supplier_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_cos_appointed_suppliers_filpride_suppliers_supplie",
                table: "filpride_cos_appointed_suppliers",
                column: "supplier_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_cos_appointed_suppliers_filpride_suppliers_supplie",
                table: "filpride_cos_appointed_suppliers");

            migrationBuilder.DropIndex(
                name: "ix_filpride_cos_appointed_suppliers_supplier_id",
                table: "filpride_cos_appointed_suppliers");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "filpride_cos_appointed_suppliers");
        }
    }
}
