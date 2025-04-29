using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeForeignKeyOldFilprideNewMobilityTheServiceInvoiceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_debit_memos_filpride_service_invoices_service_invo",
                table: "mobility_debit_memos");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_debit_memos_mobility_service_invoices_service_invo",
                table: "mobility_debit_memos",
                column: "service_invoice_id",
                principalTable: "mobility_service_invoices",
                principalColumn: "service_invoice_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_debit_memos_mobility_service_invoices_service_invo",
                table: "mobility_debit_memos");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_debit_memos_filpride_service_invoices_service_invo",
                table: "mobility_debit_memos",
                column: "service_invoice_id",
                principalTable: "filpride_service_invoices",
                principalColumn: "service_invoice_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
