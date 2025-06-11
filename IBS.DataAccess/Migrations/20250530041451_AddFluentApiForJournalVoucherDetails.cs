using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFluentApiForJournalVoucherDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_journal_voucher_details_filpride_journal_voucher_h",
                table: "filpride_journal_voucher_details");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_journal_voucher_details_filpride_journal_voucher_h",
                table: "filpride_journal_voucher_details",
                column: "journal_voucher_header_id",
                principalTable: "filpride_journal_voucher_headers",
                principalColumn: "journal_voucher_header_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_journal_voucher_details_filpride_journal_voucher_h",
                table: "filpride_journal_voucher_details");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_journal_voucher_details_filpride_journal_voucher_h",
                table: "filpride_journal_voucher_details",
                column: "journal_voucher_header_id",
                principalTable: "filpride_journal_voucher_headers",
                principalColumn: "journal_voucher_header_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
