using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNeededColumnForMobilityCustomerMasterFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "business_style",
                table: "mobility_customers",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cluster_code",
                table: "mobility_customers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "credit_limit",
                table: "mobility_customers",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "credit_limit_as_of_today",
                table: "mobility_customers",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "customer_code",
                table: "mobility_customers",
                type: "varchar(7)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_multiple_terms",
                table: "mobility_customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "retention_rate",
                table: "mobility_customers",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vat_type",
                table: "mobility_customers",
                type: "varchar(10)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "with_holding_tax",
                table: "mobility_customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "with_holding_vat",
                table: "mobility_customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "zip_code",
                table: "mobility_customers",
                type: "varchar(10)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "business_style",
                table: "mobility_customers");

            migrationBuilder.DropColumn(
                name: "cluster_code",
                table: "mobility_customers");

            migrationBuilder.DropColumn(
                name: "credit_limit",
                table: "mobility_customers");

            migrationBuilder.DropColumn(
                name: "credit_limit_as_of_today",
                table: "mobility_customers");

            migrationBuilder.DropColumn(
                name: "customer_code",
                table: "mobility_customers");

            migrationBuilder.DropColumn(
                name: "has_multiple_terms",
                table: "mobility_customers");

            migrationBuilder.DropColumn(
                name: "retention_rate",
                table: "mobility_customers");

            migrationBuilder.DropColumn(
                name: "vat_type",
                table: "mobility_customers");

            migrationBuilder.DropColumn(
                name: "with_holding_tax",
                table: "mobility_customers");

            migrationBuilder.DropColumn(
                name: "with_holding_vat",
                table: "mobility_customers");

            migrationBuilder.DropColumn(
                name: "zip_code",
                table: "mobility_customers");
        }
    }
}
