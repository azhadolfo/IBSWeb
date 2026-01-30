using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedTableForGlPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filpride_gl_period_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    period_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    fiscal_period = table.Column<int>(type: "integer", nullable: false),
                    beginning_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    debit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ending_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    adjustment_debit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    adjustment_credit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    adjusted_ending_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_gl_period_balances", x => x.id);
                    table.ForeignKey(
                        name: "fk_filpride_gl_period_balances_filpride_chart_of_accounts_acco",
                        column: x => x.account_id,
                        principalTable: "filpride_chart_of_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_gl_sub_account_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    sub_account_type = table.Column<int>(type: "integer", nullable: false),
                    sub_account_id = table.Column<int>(type: "integer", nullable: false),
                    sub_account_name = table.Column<string>(type: "varchar(200)", nullable: false),
                    period_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    fiscal_period = table.Column<int>(type: "integer", nullable: false),
                    beginning_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    debit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ending_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_gl_sub_account_balances", x => x.id);
                    table.ForeignKey(
                        name: "fk_filpride_gl_sub_account_balances_filpride_chart_of_accounts",
                        column: x => x.account_id,
                        principalTable: "filpride_chart_of_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_gl_period_balances_account_id",
                table: "filpride_gl_period_balances",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_gl_sub_account_balances_account_id",
                table: "filpride_gl_sub_account_balances",
                column: "account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filpride_gl_period_balances");

            migrationBuilder.DropTable(
                name: "filpride_gl_sub_account_balances");
        }
    }
}
