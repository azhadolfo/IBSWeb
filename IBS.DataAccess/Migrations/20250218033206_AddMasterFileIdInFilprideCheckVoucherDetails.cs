using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterFileIdInFilprideCheckVoucherDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "bank_id",
                table: "filpride_check_voucher_details",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "company_id",
                table: "filpride_check_voucher_details",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "customer_id",
                table: "filpride_check_voucher_details",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "employee_id",
                table: "filpride_check_voucher_details",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bank_id",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropColumn(
                name: "employee_id",
                table: "filpride_check_voucher_details");
        }
    }
}
