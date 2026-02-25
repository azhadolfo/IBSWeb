using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedFieldsInCV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accrued_type",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "amount_per_month",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "end_date",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "is_complete",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "last_created_date",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "number_of_months",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "number_of_months_created",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "start_date",
                table: "filpride_check_voucher_headers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "accrued_type",
                table: "filpride_check_voucher_headers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "amount_per_month",
                table: "filpride_check_voucher_headers",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateOnly>(
                name: "end_date",
                table: "filpride_check_voucher_headers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_complete",
                table: "filpride_check_voucher_headers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_created_date",
                table: "filpride_check_voucher_headers",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "number_of_months",
                table: "filpride_check_voucher_headers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "number_of_months_created",
                table: "filpride_check_voucher_headers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "start_date",
                table: "filpride_check_voucher_headers",
                type: "date",
                nullable: true);
        }
    }
}
