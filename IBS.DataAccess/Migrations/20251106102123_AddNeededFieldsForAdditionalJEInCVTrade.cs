using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNeededFieldsForAdditionalJEInCVTrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "tax_percent",
                table: "filpride_check_voucher_headers",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "tax_type",
                table: "filpride_check_voucher_headers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "vat_type",
                table: "filpride_check_voucher_headers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_display_entry",
                table: "filpride_check_voucher_details",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tax_percent",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "tax_type",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "vat_type",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "is_display_entry",
                table: "filpride_check_voucher_details");
        }
    }
}
