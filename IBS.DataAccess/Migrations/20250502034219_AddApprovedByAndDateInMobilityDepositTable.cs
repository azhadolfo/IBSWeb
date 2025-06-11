using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovedByAndDateInMobilityDepositTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "approved_by",
                table: "mobility_fms_deposits",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "approved_date",
                table: "mobility_fms_deposits",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "approved_by",
                table: "mobility_fms_deposits");

            migrationBuilder.DropColumn(
                name: "approved_date",
                table: "mobility_fms_deposits");
        }
    }
}
