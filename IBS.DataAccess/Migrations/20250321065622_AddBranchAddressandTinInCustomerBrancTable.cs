using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchAddressandTinInCustomerBrancTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_multiple_terms",
                table: "filpride_customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "branch_name",
                table: "filpride_customer_branches",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "branch_address",
                table: "filpride_customer_branches",
                type: "varchar(200)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "branch_tin",
                table: "filpride_customer_branches",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "has_multiple_terms",
                table: "filpride_customers");

            migrationBuilder.DropColumn(
                name: "branch_address",
                table: "filpride_customer_branches");

            migrationBuilder.DropColumn(
                name: "branch_tin",
                table: "filpride_customer_branches");

            migrationBuilder.AlterColumn<string>(
                name: "branch_name",
                table: "filpride_customer_branches",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)");
        }
    }
}
