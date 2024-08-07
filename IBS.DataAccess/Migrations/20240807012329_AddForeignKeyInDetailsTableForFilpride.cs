using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyInDetailsTableForFilpride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "series_number",
                table: "filpride_bank_accounts");

            migrationBuilder.AddColumn<int>(
                name: "journal_voucher_header_id",
                table: "filpride_journal_voucher_details",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "check_voucher_header_id",
                table: "filpride_check_voucher_details",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_journal_voucher_details_journal_voucher_header_id",
                table: "filpride_journal_voucher_details",
                column: "journal_voucher_header_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_details_check_voucher_header_id",
                table: "filpride_check_voucher_details",
                column: "check_voucher_header_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_check_voucher_details_filpride_check_voucher_heade",
                table: "filpride_check_voucher_details",
                column: "check_voucher_header_id",
                principalTable: "filpride_check_voucher_headers",
                principalColumn: "check_voucher_header_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_journal_voucher_details_filpride_journal_voucher_h",
                table: "filpride_journal_voucher_details",
                column: "journal_voucher_header_id",
                principalTable: "filpride_journal_voucher_headers",
                principalColumn: "journal_voucher_header_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_check_voucher_details_filpride_check_voucher_heade",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_journal_voucher_details_filpride_journal_voucher_h",
                table: "filpride_journal_voucher_details");

            migrationBuilder.DropIndex(
                name: "ix_filpride_journal_voucher_details_journal_voucher_header_id",
                table: "filpride_journal_voucher_details");

            migrationBuilder.DropIndex(
                name: "ix_filpride_check_voucher_details_check_voucher_header_id",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropColumn(
                name: "journal_voucher_header_id",
                table: "filpride_journal_voucher_details");

            migrationBuilder.DropColumn(
                name: "check_voucher_header_id",
                table: "filpride_check_voucher_details");

            migrationBuilder.AddColumn<long>(
                name: "series_number",
                table: "filpride_bank_accounts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
