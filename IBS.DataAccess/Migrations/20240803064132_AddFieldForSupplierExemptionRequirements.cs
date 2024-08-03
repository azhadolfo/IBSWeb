using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldForSupplierExemptionRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "reason_of_exemption",
                table: "filpride_suppliers",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "validity",
                table: "filpride_suppliers",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "validity_date",
                table: "filpride_suppliers",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reason_of_exemption",
                table: "filpride_suppliers");

            migrationBuilder.DropColumn(
                name: "validity",
                table: "filpride_suppliers");

            migrationBuilder.DropColumn(
                name: "validity_date",
                table: "filpride_suppliers");
        }
    }
}
