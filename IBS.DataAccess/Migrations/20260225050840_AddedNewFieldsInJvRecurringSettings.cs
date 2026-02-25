using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedNewFieldsInJvRecurringSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "expense_account",
                table: "jv_amortization_settings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "last_run_date",
                table: "jv_amortization_settings",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "next_run_date",
                table: "jv_amortization_settings",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "occurrence_remaining",
                table: "jv_amortization_settings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "prepaid_account",
                table: "jv_amortization_settings",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "expense_account",
                table: "jv_amortization_settings");

            migrationBuilder.DropColumn(
                name: "last_run_date",
                table: "jv_amortization_settings");

            migrationBuilder.DropColumn(
                name: "next_run_date",
                table: "jv_amortization_settings");

            migrationBuilder.DropColumn(
                name: "occurrence_remaining",
                table: "jv_amortization_settings");

            migrationBuilder.DropColumn(
                name: "prepaid_account",
                table: "jv_amortization_settings");
        }
    }
}
