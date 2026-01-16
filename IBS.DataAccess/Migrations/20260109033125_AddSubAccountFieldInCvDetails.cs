using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSubAccountFieldInCvDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_check_voucher_details_filpride_suppliers_supplier_",
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

            migrationBuilder.DropIndex(
                name: "ix_filpride_check_voucher_details_supplier_id",
                table: "filpride_check_voucher_details");
            //
            // migrationBuilder.DropColumn(
            //     name: "bank_id",
            //     table: "filpride_check_voucher_details");
            //
            // migrationBuilder.DropColumn(
            //     name: "company_id",
            //     table: "filpride_check_voucher_details");
            //
            // migrationBuilder.DropColumn(
            //     name: "customer_id",
            //     table: "filpride_check_voucher_details");
            //
            // migrationBuilder.DropColumn(
            //     name: "supplier_id",
            //     table: "filpride_check_voucher_details");
            //
            // migrationBuilder.DropColumn(
            //     name: "employee_id",
            //     table: "filpride_check_voucher_details");

            migrationBuilder.AddColumn<int>(
                name: "sub_account_type",
                table: "filpride_check_voucher_details",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sub_account_id",
                table: "filpride_check_voucher_details",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sub_account_name",
                table: "filpride_check_voucher_details",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // 2. Migrate Customer data (SubAccountType = 1)
            migrationBuilder.Sql(@"
                UPDATE filpride_check_voucher_details cvd
                SET
                    sub_account_type = 1,
                    sub_account_id = cvd.customer_id,
                    sub_account_name = c.customer_name
                FROM filpride_customers c
                WHERE cvd.customer_id IS NOT NULL
                  AND cvd.customer_id = c.customer_id
            ");

            // 3. Migrate Supplier data (SubAccountType = 2)
            migrationBuilder.Sql(@"
                UPDATE filpride_check_voucher_details cvd
                SET
                    sub_account_type = 2,
                    sub_account_id = cvd.supplier_id,
                    sub_account_name = s.supplier_name
                FROM filpride_suppliers s
                WHERE cvd.supplier_id IS NOT NULL
                  AND cvd.supplier_id = s.supplier_id
            ");

            // 4. Migrate Employee data (SubAccountType = 3)
            migrationBuilder.Sql(@"
                UPDATE filpride_check_voucher_details cvd
                SET
                    sub_account_type = 3,
                    sub_account_id = cvd.employee_id,
                    sub_account_name = e.first_name || ' ' || e.last_name
                FROM filpride_employees e
                WHERE cvd.employee_id IS NOT NULL
                  AND cvd.employee_id = e.employee_id
            ");

            // 5. Migrate BankAccount data (SubAccountType = 4)
            migrationBuilder.Sql(@"
                UPDATE filpride_check_voucher_details cvd
                SET
                    sub_account_type = 4,
                    sub_account_id = cvd.bank_id,
                    sub_account_name = b.account_no || ' ' || b.account_name
                FROM filpride_bank_accounts b
                WHERE cvd.bank_id IS NOT NULL
                  AND cvd.bank_id = b.bank_account_id
            ");

            // 6. Migrate Company data (SubAccountType = 5)
            migrationBuilder.Sql(@"
                UPDATE filpride_check_voucher_details cvd
                SET
                    sub_account_type = 5,
                    sub_account_id = cvd.company_id,
                    sub_account_name = c.company_name
                FROM companies c
                WHERE cvd.company_id IS NOT NULL
                  AND cvd.company_id = c.company_id
            ");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_details_sub_account",
                table: "filpride_check_voucher_details",
                columns: new[] { "sub_account_type", "sub_account_id" });

            // To update actual payee for payroll
            migrationBuilder.Sql(@"
                UPDATE filpride_check_voucher_headers AS header
                SET payee = details.sub_account_name
                FROM public.filpride_check_voucher_details AS details
                WHERE details.check_voucher_header_id = header.check_voucher_header_id
                  AND details.account_no = '202010200'
                  AND header.is_payroll = TRUE
                  AND details.credit > 0;
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            // migrationBuilder.AddColumn<int>(
            //     name: "bank_id",
            //     table: "filpride_check_voucher_details",
            //     type: "integer",
            //     nullable: true);
            //
            // migrationBuilder.AddColumn<int>(
            //     name: "company_id",
            //     table: "filpride_check_voucher_details",
            //     type: "integer",
            //     nullable: true);
            //
            // migrationBuilder.AddColumn<int>(
            //     name: "customer_id",
            //     table: "filpride_check_voucher_details",
            //     type: "integer",
            //     nullable: true);
            //
            // migrationBuilder.AddColumn<int>(
            //     name: "supplier_id",
            //     table: "filpride_check_voucher_details",
            //     type: "integer",
            //     nullable: true);
            //
            // migrationBuilder.AddColumn<int>(
            //     name: "employee_id",
            //     table: "filpride_check_voucher_details",
            //     type: "integer",
            //     nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_details_supplier_id",
                table: "filpride_check_voucher_details",
                column: "supplier_id");

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

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_check_voucher_details_filpride_suppliers_supplier_",
                table: "filpride_check_voucher_details",
                column: "supplier_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "ix_filpride_check_voucher_details_sub_account",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropIndex(
                name: "ix_filpride_check_voucher_details_sub_account_code",
                table: "filpride_check_voucher_details");

            // Drop new columns
            migrationBuilder.DropColumn(
                name: "sub_account_type",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropColumn(
                name: "sub_account_id",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropColumn(
                name: "sub_account_code",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropColumn(
                name: "sub_account_name",
                table: "filpride_check_voucher_details");
        }
    }
}
