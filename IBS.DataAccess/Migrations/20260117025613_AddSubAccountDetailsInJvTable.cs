using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSubAccountDetailsInJvTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sub_account_id",
                table: "filpride_journal_voucher_details",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sub_account_name",
                table: "filpride_journal_voucher_details",
                type: "varchar(200)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sub_account_type",
                table: "filpride_journal_voucher_details",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customers_commissionee_id",
                table: "filpride_customers",
                column: "commissionee_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customers_filpride_suppliers_commissionee_id",
                table: "filpride_customers",
                column: "commissionee_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id");

            migrationBuilder.Sql(@"
                UPDATE public.filpride_journal_voucher_details details
                SET
                    sub_account_type = 3,
                    sub_account_id = cv.employee_id,
                    sub_account_name = cv.payee
                FROM public.filpride_journal_voucher_headers header
                LEFT JOIN public.filpride_check_voucher_headers cv
                    ON header.cv_id = cv.check_voucher_header_id
                WHERE details.journal_voucher_header_id = header.journal_voucher_header_id
                  AND details.account_no = '101020400'
                  AND cv.employee_id IS NOT NULL;
            ");

            migrationBuilder.Sql(@"
                update public.filpride_journal_voucher_details
                set account_name = 'Advances to Officers and Employees'
                where account_name = 'Advances from Officers and Employees'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customers_filpride_suppliers_commissionee_id",
                table: "filpride_customers");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customers_commissionee_id",
                table: "filpride_customers");

            migrationBuilder.DropColumn(
                name: "sub_account_id",
                table: "filpride_journal_voucher_details");

            migrationBuilder.DropColumn(
                name: "sub_account_name",
                table: "filpride_journal_voucher_details");

            migrationBuilder.DropColumn(
                name: "sub_account_type",
                table: "filpride_journal_voucher_details");
        }
    }
}
