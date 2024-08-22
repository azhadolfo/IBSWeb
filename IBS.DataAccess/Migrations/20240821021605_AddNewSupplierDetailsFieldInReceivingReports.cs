using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNewSupplierDetailsFieldInReceivingReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "supplier_dr_date",
                table: "receiving_reports",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_dr_no",
                table: "receiving_reports",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "withdrawal_certificate",
                table: "receiving_reports",
                type: "varchar(50)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "supplier_dr_date",
                table: "receiving_reports");

            migrationBuilder.DropColumn(
                name: "supplier_dr_no",
                table: "receiving_reports");

            migrationBuilder.DropColumn(
                name: "withdrawal_certificate",
                table: "receiving_reports");
        }
    }
}
