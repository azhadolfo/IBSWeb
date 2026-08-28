using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCompanyFromMonthlyNibit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_filpride_monthly_nibits_company",
                table: "filpride_monthly_nibits");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_monthly_nibits");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_monthly_nibits",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_monthly_nibits_company",
                table: "filpride_monthly_nibits",
                column: "company");
        }
    }
}
