using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAndModifiedFilprideChartOfAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create a new integer column
            migrationBuilder.AddColumn<int>(
                name: "parent_account_id_temp",
                table: "filpride_chart_of_accounts",
                type: "integer",
                nullable: true);

            // Step 2: Copy data from "parent" (string) to "parent_account_id_temp" (integer), ignoring non-numeric values
            migrationBuilder.Sql(
                "UPDATE filpride_chart_of_accounts " +
                "SET parent_account_id_temp = NULLIF(parent, '')::INTEGER " +
                "WHERE parent ~ '^[0-9]+$'");

            // Step 3: Drop the old column
            migrationBuilder.DropColumn(
                name: "parent",
                table: "filpride_chart_of_accounts");

            // Step 4: Rename the temporary column to the correct name
            migrationBuilder.RenameColumn(
                name: "parent_account_id_temp",
                table: "filpride_chart_of_accounts",
                newName: "parent_account_id");

            // Step 5: Create an index
            migrationBuilder.CreateIndex(
                name: "ix_filpride_chart_of_accounts_parent_account_id",
                table: "filpride_chart_of_accounts",
                column: "parent_account_id");

            // Step 6: Add the foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "fk_filpride_chart_of_accounts_filpride_chart_of_accounts_parent",
                table: "filpride_chart_of_accounts",
                column: "parent_account_id",
                principalTable: "filpride_chart_of_accounts",
                principalColumn: "account_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse process in case of rollback

            // Step 1: Drop foreign key and index
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_chart_of_accounts_filpride_chart_of_accounts_parent",
                table: "filpride_chart_of_accounts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_chart_of_accounts_parent_account_id",
                table: "filpride_chart_of_accounts");

            // Step 2: Add the old string column back
            migrationBuilder.AddColumn<string>(
                name: "parent",
                table: "filpride_chart_of_accounts",
                type: "varchar(15)",
                nullable: true);

            // Step 3: Convert back integer values to string
            migrationBuilder.Sql(
                "UPDATE filpride_chart_of_accounts " +
                "SET parent = parent_account_id::VARCHAR " +
                "WHERE parent_account_id IS NOT NULL");

            // Step 4: Drop the new integer column
            migrationBuilder.DropColumn(
                name: "parent_account_id",
                table: "filpride_chart_of_accounts");
        }
    }
}
