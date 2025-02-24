using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyIdInGeneralLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "company_id",
                table: "filpride_general_ledger_books",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_general_ledger_books_company_id",
                table: "filpride_general_ledger_books",
                column: "company_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_general_ledger_books_companies_company_id",
                table: "filpride_general_ledger_books",
                column: "company_id",
                principalTable: "companies",
                principalColumn: "company_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_general_ledger_books_companies_company_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropIndex(
                name: "ix_filpride_general_ledger_books_company_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "filpride_general_ledger_books");
        }
    }
}
