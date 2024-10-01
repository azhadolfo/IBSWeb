using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateFilprideChartOfAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filpride_chart_of_accounts",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    is_main = table.Column<bool>(type: "boolean", nullable: false),
                    account_number = table.Column<string>(type: "varchar(15)", nullable: true),
                    account_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    account_type = table.Column<string>(type: "varchar(25)", nullable: true),
                    normal_balance = table.Column<string>(type: "varchar(20)", nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    parent = table.Column<string>(type: "varchar(15)", nullable: true),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_chart_of_accounts", x => x.account_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_chart_of_accounts_account_name",
                table: "filpride_chart_of_accounts",
                column: "account_name");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_chart_of_accounts_account_number",
                table: "filpride_chart_of_accounts",
                column: "account_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filpride_chart_of_accounts");
        }
    }
}
