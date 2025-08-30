using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddManagersCheckFieldsToCrModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "managers_check_amount",
                table: "filpride_collection_receipts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "managers_check_bank",
                table: "filpride_collection_receipts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "managers_check_branch",
                table: "filpride_collection_receipts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "managers_check_date",
                table: "filpride_collection_receipts",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "managers_check_no",
                table: "filpride_collection_receipts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "managers_check_amount",
                table: "filpride_collection_receipts");

            migrationBuilder.DropColumn(
                name: "managers_check_bank",
                table: "filpride_collection_receipts");

            migrationBuilder.DropColumn(
                name: "managers_check_branch",
                table: "filpride_collection_receipts");

            migrationBuilder.DropColumn(
                name: "managers_check_date",
                table: "filpride_collection_receipts");

            migrationBuilder.DropColumn(
                name: "managers_check_no",
                table: "filpride_collection_receipts");
        }
    }
}
