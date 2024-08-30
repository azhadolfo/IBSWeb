using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RollBackTheSettingCVIdRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_journal_voucher_headers_filpride_check_voucher_hea",
                table: "filpride_journal_voucher_headers");

            migrationBuilder.AlterColumn<int>(
                name: "cv_id",
                table: "filpride_journal_voucher_headers",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_journal_voucher_headers_filpride_check_voucher_hea",
                table: "filpride_journal_voucher_headers",
                column: "cv_id",
                principalTable: "filpride_check_voucher_headers",
                principalColumn: "check_voucher_header_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_journal_voucher_headers_filpride_check_voucher_hea",
                table: "filpride_journal_voucher_headers");

            migrationBuilder.AlterColumn<int>(
                name: "cv_id",
                table: "filpride_journal_voucher_headers",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_journal_voucher_headers_filpride_check_voucher_hea",
                table: "filpride_journal_voucher_headers",
                column: "cv_id",
                principalTable: "filpride_check_voucher_headers",
                principalColumn: "check_voucher_header_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
