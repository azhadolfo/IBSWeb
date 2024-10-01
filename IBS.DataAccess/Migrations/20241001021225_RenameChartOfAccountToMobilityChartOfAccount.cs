using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameChartOfAccountToMobilityChartOfAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing indexes
            migrationBuilder.DropIndex(
                name: "ix_chart_of_accounts_account_name",
                table: "chart_of_accounts");

            migrationBuilder.DropIndex(
                name: "ix_chart_of_accounts_account_number",
                table: "chart_of_accounts");

            // Rename the table
            migrationBuilder.RenameTable(
                name: "chart_of_accounts",
                newName: "mobility_chart_of_accounts");

            // Add new indexes
            migrationBuilder.CreateIndex(
                name: "ix_mobility_chart_of_accounts_account_name",
                table: "mobility_chart_of_accounts",
                column: "account_name");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_chart_of_accounts_account_number",
                table: "mobility_chart_of_accounts",
                column: "account_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop newly created indexes
            migrationBuilder.DropIndex(
                name: "ix_mobility_chart_of_accounts_account_name",
                table: "mobility_chart_of_accounts");

            migrationBuilder.DropIndex(
                name: "ix_mobility_chart_of_accounts_account_number",
                table: "mobility_chart_of_accounts");

            // Rename the table back
            migrationBuilder.RenameTable(
                name: "mobility_chart_of_accounts",
                newName: "chart_of_accounts");

            // Recreate original indexes
            migrationBuilder.CreateIndex(
                name: "ix_chart_of_accounts_account_name",
                table: "chart_of_accounts",
                column: "account_name");

            migrationBuilder.CreateIndex(
                name: "ix_chart_of_accounts_account_number",
                table: "chart_of_accounts",
                column: "account_number",
                unique: true);
        }
    }
}
