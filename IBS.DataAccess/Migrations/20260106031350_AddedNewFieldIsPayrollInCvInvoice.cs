using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedNewFieldIsPayrollInCvInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customers_filpride_suppliers_commissionee_id",
                table: "filpride_customers");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customers_commissionee_id",
                table: "filpride_customers");

            migrationBuilder.AddColumn<bool>(
                name: "is_payroll",
                table: "filpride_check_voucher_headers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Update the record
            migrationBuilder.Sql(@"
                update public.filpride_check_voucher_headers
                set is_payroll = true
                where supplier_id is null and cv_Type = 'Invoicing';
            ");

            // To update null payee
            migrationBuilder.Sql(@"
                update public.filpride_check_voucher_headers
                set payee = 'FILPRIDE RESOURCES, INC.'
                where payee is null;
            ");

            // To update ap-non trade payable for invoice
            migrationBuilder.Sql(@"
                update public.filpride_check_voucher_details
                set amount = credit
                where transaction_no like 'INV%'
	                and account_no = '202010200'
	                and credit > 0 and amount = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_payroll",
                table: "filpride_check_voucher_headers");

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
        }
    }
}
