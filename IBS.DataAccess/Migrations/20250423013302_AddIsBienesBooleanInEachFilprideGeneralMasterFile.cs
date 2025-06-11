using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIsBienesBooleanInEachFilprideGeneralMasterFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_bienes",
                table: "filpride_suppliers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_bienes",
                table: "filpride_services",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_bienes",
                table: "filpride_pick_up_points",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_bienes",
                table: "filpride_customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_bienes",
                table: "filpride_bank_accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_bienes",
                table: "filpride_suppliers");

            migrationBuilder.DropColumn(
                name: "is_bienes",
                table: "filpride_services");

            migrationBuilder.DropColumn(
                name: "is_bienes",
                table: "filpride_pick_up_points");

            migrationBuilder.DropColumn(
                name: "is_bienes",
                table: "filpride_customers");

            migrationBuilder.DropColumn(
                name: "is_bienes",
                table: "filpride_bank_accounts");
        }
    }
}
