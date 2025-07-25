using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTheCollectionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "manager_check_amount",
                table: "filpride_collection_receipts");

            migrationBuilder.DropColumn(
                name: "manager_check_bank",
                table: "filpride_collection_receipts");

            migrationBuilder.DropColumn(
                name: "manager_check_branch",
                table: "filpride_collection_receipts");

            migrationBuilder.DropColumn(
                name: "manager_check_date",
                table: "filpride_collection_receipts");

            migrationBuilder.DropColumn(
                name: "manager_check_no",
                table: "filpride_collection_receipts");

            migrationBuilder.AlterColumn<string>(
                name: "payment_status",
                table: "filpride_sales_invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "payment_status",
                table: "filpride_sales_invoices",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<decimal>(
                name: "manager_check_amount",
                table: "filpride_collection_receipts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "manager_check_bank",
                table: "filpride_collection_receipts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "manager_check_branch",
                table: "filpride_collection_receipts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "manager_check_date",
                table: "filpride_collection_receipts",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "manager_check_no",
                table: "filpride_collection_receipts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
