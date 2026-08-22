using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveColumnIsAutoReverseNextMonthProcessed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_auto_reverse_next_month_processed",
                table: "filpride_journal_voucher_headers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_auto_reverse_next_month_processed",
                table: "filpride_journal_voucher_headers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
