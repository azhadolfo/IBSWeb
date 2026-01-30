using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedTheGLTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns
            migrationBuilder.AddColumn<int>(
                name: "sub_account_type",
                table: "filpride_general_ledger_books",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sub_account_id",
                table: "filpride_general_ledger_books",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sub_account_name",
                table: "filpride_general_ledger_books",
                type: "varchar(200)",
                nullable: true);

            // Migrate existing data - Customer
            migrationBuilder.Sql(@"
                UPDATE filpride_general_ledger_books
                SET sub_account_type = 1,
                    sub_account_id = customer_id,
                    sub_account_name = customer_name
                WHERE customer_id IS NOT NULL;
            ");

            // Migrate existing data - Supplier
            migrationBuilder.Sql(@"
                UPDATE filpride_general_ledger_books
                SET sub_account_type = 2,
                    sub_account_id = supplier_id,
                    sub_account_name = supplier_name
                WHERE supplier_id IS NOT NULL;
            ");

            // Migrate existing data - Employee
            migrationBuilder.Sql(@"
                UPDATE filpride_general_ledger_books
                SET sub_account_type = 3,
                    sub_account_id = employee_id,
                    sub_account_name = employee_name
                WHERE employee_id IS NOT NULL;
            ");

            // Migrate existing data - Bank Account
            migrationBuilder.Sql(@"
                UPDATE filpride_general_ledger_books
                SET sub_account_type = 4,
                    sub_account_id = bank_account_id,
                    sub_account_name = bank_account_name
                WHERE bank_account_id IS NOT NULL;
            ");

            // Migrate existing data - Company
            migrationBuilder.Sql(@"
                UPDATE filpride_general_ledger_books
                SET sub_account_type = 5,
                    sub_account_id = company_id,
                    sub_account_name = company_name
                WHERE company_id IS NOT NULL;
            ");


            // Add indexes
            migrationBuilder.CreateIndex(
                name: "ix_filpride_general_ledger_books_sub_account",
                table: "filpride_general_ledger_books",
                columns: new[] { "sub_account_type", "sub_account_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "sub_account_type", table: "filpride_general_ledger_books");
            migrationBuilder.DropColumn(name: "sub_account_id", table: "filpride_general_ledger_books");
            migrationBuilder.DropColumn(name: "sub_account_code", table: "filpride_general_ledger_books");
            migrationBuilder.DropColumn(name: "sub_account_name", table: "filpride_general_ledger_books");
        }
    }
}
