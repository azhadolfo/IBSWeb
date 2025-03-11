using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeForeignKeyInCheckVoucherTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "employee_id",
                table: "filpride_check_voucher_headers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_advances",
                table: "filpride_check_voucher_headers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_headers_employee_id",
                table: "filpride_check_voucher_headers",
                column: "employee_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_check_voucher_headers_filpride_employees_employee_",
                table: "filpride_check_voucher_headers",
                column: "employee_id",
                principalTable: "filpride_employees",
                principalColumn: "employee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_check_voucher_headers_filpride_employees_employee_",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropIndex(
                name: "ix_filpride_check_voucher_headers_employee_id",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "employee_id",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "is_advances",
                table: "filpride_check_voucher_headers");
        }
    }
}
