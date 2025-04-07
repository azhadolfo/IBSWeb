using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNeededFieldsForSupplierInMobility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "branch",
                table: "mobility_suppliers",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "mobility_suppliers",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "mobility_suppliers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "mobility_suppliers",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_date",
                table: "mobility_suppliers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "default_expense_number",
                table: "mobility_suppliers",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "edited_by",
                table: "mobility_suppliers",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "edited_date",
                table: "mobility_suppliers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "mobility_suppliers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "proof_of_exemption_file_name",
                table: "mobility_suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "proof_of_exemption_file_path",
                table: "mobility_suppliers",
                type: "varchar(1024)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "proof_of_registration_file_name",
                table: "mobility_suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "proof_of_registration_file_path",
                table: "mobility_suppliers",
                type: "varchar(1024)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reason_of_exemption",
                table: "mobility_suppliers",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_code",
                table: "mobility_suppliers",
                type: "varchar(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_terms",
                table: "mobility_suppliers",
                type: "varchar(3)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "supplier_tin",
                table: "mobility_suppliers",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tax_type",
                table: "mobility_suppliers",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "trade_name",
                table: "mobility_suppliers",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "validity",
                table: "mobility_suppliers",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "validity_date",
                table: "mobility_suppliers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vat_type",
                table: "mobility_suppliers",
                type: "varchar(10)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "withholding_tax_percent",
                table: "mobility_suppliers",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "withholding_taxtitle",
                table: "mobility_suppliers",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "zip_code",
                table: "mobility_suppliers",
                type: "varchar(10)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "branch",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "category",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "company",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "created_date",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "default_expense_number",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "edited_by",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "edited_date",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "proof_of_exemption_file_name",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "proof_of_exemption_file_path",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "proof_of_registration_file_name",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "proof_of_registration_file_path",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "reason_of_exemption",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "supplier_code",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "supplier_terms",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "supplier_tin",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "tax_type",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "trade_name",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "validity",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "validity_date",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "vat_type",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "withholding_tax_percent",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "withholding_taxtitle",
                table: "mobility_suppliers");

            migrationBuilder.DropColumn(
                name: "zip_code",
                table: "mobility_suppliers");
        }
    }
}
