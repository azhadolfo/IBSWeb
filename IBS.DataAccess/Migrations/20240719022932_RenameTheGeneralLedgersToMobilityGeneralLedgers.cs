using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameTheGeneralLedgersToMobilityGeneralLedgers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_general_ledgers",
                table: "general_ledgers");

            migrationBuilder.RenameTable(
                name: "general_ledgers",
                newName: "mobility_general_ledgers");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_transaction_date",
                table: "mobility_general_ledgers",
                newName: "ix_mobility_general_ledgers_transaction_date");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_supplier_code",
                table: "mobility_general_ledgers",
                newName: "ix_mobility_general_ledgers_supplier_code");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_station_code",
                table: "mobility_general_ledgers",
                newName: "ix_mobility_general_ledgers_station_code");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_reference",
                table: "mobility_general_ledgers",
                newName: "ix_mobility_general_ledgers_reference");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_product_code",
                table: "mobility_general_ledgers",
                newName: "ix_mobility_general_ledgers_product_code");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_journal_reference",
                table: "mobility_general_ledgers",
                newName: "ix_mobility_general_ledgers_journal_reference");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_customer_code",
                table: "mobility_general_ledgers",
                newName: "ix_mobility_general_ledgers_customer_code");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_account_title",
                table: "mobility_general_ledgers",
                newName: "ix_mobility_general_ledgers_account_title");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_account_number",
                table: "mobility_general_ledgers",
                newName: "ix_mobility_general_ledgers_account_number");

            migrationBuilder.AddPrimaryKey(
                name: "pk_mobility_general_ledgers",
                table: "mobility_general_ledgers",
                column: "general_ledger_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_mobility_general_ledgers",
                table: "mobility_general_ledgers");

            migrationBuilder.RenameTable(
                name: "mobility_general_ledgers",
                newName: "general_ledgers");

            migrationBuilder.RenameIndex(
                name: "ix_mobility_general_ledgers_transaction_date",
                table: "general_ledgers",
                newName: "ix_general_ledgers_transaction_date");

            migrationBuilder.RenameIndex(
                name: "ix_mobility_general_ledgers_supplier_code",
                table: "general_ledgers",
                newName: "ix_general_ledgers_supplier_code");

            migrationBuilder.RenameIndex(
                name: "ix_mobility_general_ledgers_station_code",
                table: "general_ledgers",
                newName: "ix_general_ledgers_station_code");

            migrationBuilder.RenameIndex(
                name: "ix_mobility_general_ledgers_reference",
                table: "general_ledgers",
                newName: "ix_general_ledgers_reference");

            migrationBuilder.RenameIndex(
                name: "ix_mobility_general_ledgers_product_code",
                table: "general_ledgers",
                newName: "ix_general_ledgers_product_code");

            migrationBuilder.RenameIndex(
                name: "ix_mobility_general_ledgers_journal_reference",
                table: "general_ledgers",
                newName: "ix_general_ledgers_journal_reference");

            migrationBuilder.RenameIndex(
                name: "ix_mobility_general_ledgers_customer_code",
                table: "general_ledgers",
                newName: "ix_general_ledgers_customer_code");

            migrationBuilder.RenameIndex(
                name: "ix_mobility_general_ledgers_account_title",
                table: "general_ledgers",
                newName: "ix_general_ledgers_account_title");

            migrationBuilder.RenameIndex(
                name: "ix_mobility_general_ledgers_account_number",
                table: "general_ledgers",
                newName: "ix_general_ledgers_account_number");

            migrationBuilder.AddPrimaryKey(
                name: "PK_general_ledgers",
                table: "general_ledgers",
                column: "general_ledger_id");
        }
    }
}
