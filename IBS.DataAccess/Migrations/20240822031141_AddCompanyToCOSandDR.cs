using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyToCOSandDR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "supplier_dr_date",
                table: "filpride_receiving_reports");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_delivery_receipts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_customer_order_slips",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_customer_order_slips");

            migrationBuilder.AddColumn<DateOnly>(
                name: "supplier_dr_date",
                table: "filpride_receiving_reports",
                type: "date",
                nullable: true);
        }
    }
}
