using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyNibitsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filpride_monthly_nibits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    beginning_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    net_income = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    prior_period_adjustment = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ending_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_monthly_nibits", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_monthly_nibits_company",
                table: "filpride_monthly_nibits",
                column: "company");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_monthly_nibits_month",
                table: "filpride_monthly_nibits",
                column: "month");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_monthly_nibits_year",
                table: "filpride_monthly_nibits",
                column: "year");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filpride_monthly_nibits");
        }
    }
}
