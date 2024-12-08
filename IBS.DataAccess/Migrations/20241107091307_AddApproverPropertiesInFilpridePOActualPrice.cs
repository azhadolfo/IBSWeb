using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddApproverPropertiesInFilpridePOActualPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "approved_by",
                table: "filpride_po_actual_prices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "approved_date",
                table: "filpride_po_actual_prices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_approved",
                table: "filpride_po_actual_prices",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "approved_by",
                table: "filpride_po_actual_prices");

            migrationBuilder.DropColumn(
                name: "approved_date",
                table: "filpride_po_actual_prices");

            migrationBuilder.DropColumn(
                name: "is_approved",
                table: "filpride_po_actual_prices");
        }
    }
}
