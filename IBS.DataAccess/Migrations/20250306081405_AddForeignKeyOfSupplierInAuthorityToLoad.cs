using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyOfSupplierInAuthorityToLoad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_authority_to_loads_filpride_delivery_receipts_deli",
                table: "filpride_authority_to_loads");

            migrationBuilder.DropIndex(
                name: "ix_filpride_authority_to_loads_delivery_receipt_id",
                table: "filpride_authority_to_loads");

            migrationBuilder.DropColumn(
                name: "delivery_receipt_id",
                table: "filpride_authority_to_loads");

            migrationBuilder.AddColumn<int>(
                name: "supplier_id",
                table: "filpride_authority_to_loads",
                type: "integer",
                nullable: false,
                defaultValue: 19);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_authority_to_loads_supplier_id",
                table: "filpride_authority_to_loads",
                column: "supplier_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_authority_to_loads_filpride_suppliers_supplier_id",
                table: "filpride_authority_to_loads",
                column: "supplier_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_authority_to_loads_filpride_suppliers_supplier_id",
                table: "filpride_authority_to_loads");

            migrationBuilder.DropIndex(
                name: "ix_filpride_authority_to_loads_supplier_id",
                table: "filpride_authority_to_loads");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "filpride_authority_to_loads");

            migrationBuilder.AddColumn<int>(
                name: "delivery_receipt_id",
                table: "filpride_authority_to_loads",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_authority_to_loads_delivery_receipt_id",
                table: "filpride_authority_to_loads",
                column: "delivery_receipt_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_authority_to_loads_filpride_delivery_receipts_deli",
                table: "filpride_authority_to_loads",
                column: "delivery_receipt_id",
                principalTable: "filpride_delivery_receipts",
                principalColumn: "delivery_receipt_id");
        }
    }
}
