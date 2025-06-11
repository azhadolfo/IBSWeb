using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCreateByAndEditByInDataEntryModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "update_by",
                table: "mmsi_tariff_rates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "update_date",
                table: "mmsi_tariff_rates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_date",
                table: "mmsi_dispatch_tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "edited_by",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "edited_date",
                table: "mmsi_dispatch_tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tariff_by",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "tariff_date",
                table: "mmsi_dispatch_tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tariff_edited_by",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "tariff_edited_date",
                table: "mmsi_dispatch_tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "mmsi_collections",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_date",
                table: "mmsi_collections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "mmsi_billings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_date",
                table: "mmsi_billings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_edited_by",
                table: "mmsi_billings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_edited_date",
                table: "mmsi_billings",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "update_by",
                table: "mmsi_tariff_rates");

            migrationBuilder.DropColumn(
                name: "update_date",
                table: "mmsi_tariff_rates");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropColumn(
                name: "created_date",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropColumn(
                name: "edited_by",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropColumn(
                name: "edited_date",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropColumn(
                name: "tariff_by",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropColumn(
                name: "tariff_date",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropColumn(
                name: "tariff_edited_by",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropColumn(
                name: "tariff_edited_date",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "mmsi_collections");

            migrationBuilder.DropColumn(
                name: "created_date",
                table: "mmsi_collections");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "mmsi_billings");

            migrationBuilder.DropColumn(
                name: "created_date",
                table: "mmsi_billings");

            migrationBuilder.DropColumn(
                name: "last_edited_by",
                table: "mmsi_billings");

            migrationBuilder.DropColumn(
                name: "last_edited_date",
                table: "mmsi_billings");
        }
    }
}
