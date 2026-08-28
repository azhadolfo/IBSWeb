using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCompanyFromFilprideMasterFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_filpride_pick_up_points_company",
                table: "filpride_pick_up_points");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_suppliers");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_services");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_pick_up_points");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_customers");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_bank_accounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_suppliers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_services",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_pick_up_points",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_bank_accounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_pick_up_points_company",
                table: "filpride_pick_up_points",
                column: "company");
        }
    }
}
