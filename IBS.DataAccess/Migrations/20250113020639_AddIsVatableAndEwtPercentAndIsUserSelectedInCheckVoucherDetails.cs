using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIsVatableAndEwtPercentAndIsUserSelectedInCheckVoucherDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ewt_percent",
                table: "filpride_check_voucher_details",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "is_user_selected",
                table: "filpride_check_voucher_details",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_vatable",
                table: "filpride_check_voucher_details",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ewt_percent",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropColumn(
                name: "is_user_selected",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropColumn(
                name: "is_vatable",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "filpride_check_voucher_details");
        }
    }
}
