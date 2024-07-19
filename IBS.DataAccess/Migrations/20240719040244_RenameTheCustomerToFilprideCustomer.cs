using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameTheCustomerToFilprideCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraints that reference the old table
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_customers_customer_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_delivery_receipts_customers_customer_id",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_receiving_reports_customers_customer_id",
                table: "filpride_receiving_reports");

            // Rename the table from "customers" to "filpride_customers"
            migrationBuilder.RenameTable(
                name: "customers",
                newName: "filpride_customers");

            // Rename indexes if necessary
            migrationBuilder.RenameIndex(
                name: "ix_customers_customer_code",
                table: "filpride_customers",
                newName: "ix_filpride_customers_customer_code");

            migrationBuilder.RenameIndex(
                name: "ix_customers_customer_name",
                table: "filpride_customers",
                newName: "ix_filpride_customers_customer_name");

            // Add new foreign key constraints that reference the new table
            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_customers_customer_id",
                table: "filpride_customer_order_slips",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_customers_customer_id",
                table: "filpride_delivery_receipts",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_receiving_reports_filpride_customers_customer_id",
                table: "filpride_receiving_reports",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraints that reference the renamed table
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_customers_customer_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_customers_customer_id",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_receiving_reports_filpride_customers_customer_id",
                table: "filpride_receiving_reports");

            // Rename the table back from "filpride_customers" to "customers"
            migrationBuilder.RenameTable(
                name: "filpride_customers",
                newName: "customers");

            // Rename indexes if necessary
            migrationBuilder.RenameIndex(
                name: "ix_filpride_customers_customer_code",
                table: "customers",
                newName: "ix_customers_customer_code");

            migrationBuilder.RenameIndex(
                name: "ix_filpride_customers_customer_name",
                table: "customers",
                newName: "ix_customers_customer_name");

            // Add foreign key constraints that reference the original table
            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_customers_customer_id",
                table: "filpride_customer_order_slips",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_delivery_receipts_customers_customer_id",
                table: "filpride_delivery_receipts",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_receiving_reports_customers_customer_id",
                table: "filpride_receiving_reports",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
