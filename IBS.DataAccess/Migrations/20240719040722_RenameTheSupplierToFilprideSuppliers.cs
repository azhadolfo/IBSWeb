using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameTheSupplierToFilprideSuppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraints that reference the old table
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_purchase_orders_suppliers_supplier_id",
                table: "filpride_purchase_orders");

            // Rename the table from "suppliers" to "filpride_suppliers"
            migrationBuilder.RenameTable(
                name: "suppliers",
                newName: "filpride_suppliers");

            // Rename indexes if necessary
            migrationBuilder.RenameIndex(
                name: "ix_suppliers_supplier_code",
                table: "filpride_suppliers",
                newName: "ix_filpride_suppliers_supplier_code");

            migrationBuilder.RenameIndex(
                name: "ix_suppliers_supplier_name",
                table: "filpride_suppliers",
                newName: "ix_filpride_suppliers_supplier_name");

            // Add new foreign key constraints that reference the new table
            migrationBuilder.AddForeignKey(
                name: "fk_filpride_purchase_orders_filpride_suppliers_supplier_id",
                table: "filpride_purchase_orders",
                column: "supplier_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraints that reference the renamed table
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_purchase_orders_filpride_suppliers_supplier_id",
                table: "filpride_purchase_orders");

            // Rename the table back from "filpride_suppliers" to "suppliers"
            migrationBuilder.RenameTable(
                name: "filpride_suppliers",
                newName: "suppliers");

            // Rename indexes if necessary
            migrationBuilder.RenameIndex(
                name: "ix_filpride_suppliers_supplier_code",
                table: "suppliers",
                newName: "ix_suppliers_supplier_code");

            migrationBuilder.RenameIndex(
                name: "ix_filpride_suppliers_supplier_name",
                table: "suppliers",
                newName: "ix_suppliers_supplier_name");

            // Add foreign key constraints that reference the original table
            migrationBuilder.AddForeignKey(
                name: "fk_filpride_purchase_orders_suppliers_supplier_id",
                table: "filpride_purchase_orders",
                column: "supplier_id",
                principalTable: "suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
