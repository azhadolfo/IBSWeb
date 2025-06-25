using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedJoinEntitiesToCheckVoucherDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_details_bank_id",
                table: "filpride_check_voucher_details",
                column: "bank_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_details_company_id",
                table: "filpride_check_voucher_details",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_details_customer_id",
                table: "filpride_check_voucher_details",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_details_employee_id",
                table: "filpride_check_voucher_details",
                column: "employee_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_check_voucher_details_companies_company_id",
                table: "filpride_check_voucher_details",
                column: "company_id",
                principalTable: "companies",
                principalColumn: "company_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_check_voucher_details_filpride_bank_accounts_bank_",
                table: "filpride_check_voucher_details",
                column: "bank_id",
                principalTable: "filpride_bank_accounts",
                principalColumn: "bank_account_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_check_voucher_details_filpride_customers_customer_",
                table: "filpride_check_voucher_details",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_check_voucher_details_filpride_employees_employee_",
                table: "filpride_check_voucher_details",
                column: "employee_id",
                principalTable: "filpride_employees",
                principalColumn: "employee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_check_voucher_details_companies_company_id",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_check_voucher_details_filpride_bank_accounts_bank_",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_check_voucher_details_filpride_customers_customer_",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_check_voucher_details_filpride_employees_employee_",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropIndex(
                name: "ix_filpride_check_voucher_details_bank_id",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropIndex(
                name: "ix_filpride_check_voucher_details_company_id",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropIndex(
                name: "ix_filpride_check_voucher_details_customer_id",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropIndex(
                name: "ix_filpride_check_voucher_details_employee_id",
                table: "filpride_check_voucher_details");
        }
    }
}
