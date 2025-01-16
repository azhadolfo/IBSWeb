using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNeededFieldsForImplementingMultipleEntryOfSupplierInInvoicingAndPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "total",
                table: "filpride_check_voucher_headers",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                table: "filpride_check_voucher_details",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "amount_paid",
                table: "filpride_check_voucher_details",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "supplier_id",
                table: "filpride_check_voucher_details",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_details_supplier_id",
                table: "filpride_check_voucher_details",
                column: "supplier_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_check_voucher_details_filpride_suppliers_supplier_",
                table: "filpride_check_voucher_details",
                column: "supplier_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_check_voucher_details_filpride_suppliers_supplier_",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropIndex(
                name: "ix_filpride_check_voucher_details_supplier_id",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropColumn(
                name: "amount",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropColumn(
                name: "amount_paid",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "filpride_check_voucher_details");

            migrationBuilder.AlterColumn<decimal>(
                name: "total",
                table: "filpride_check_voucher_headers",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");
        }
    }
}
