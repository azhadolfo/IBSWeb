using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ModifyDispatchTicketModelFieldsToNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_services_service_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_terminals_terminal_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_tug_masters_tug_master_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_tugboats_tug_boat_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_vessels_vessel_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.AlterColumn<int>(
                name: "vessel_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "tug_master_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "tug_boat_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "time_left",
                table: "mmsi_dispatch_tickets",
                type: "time without time zone",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "time_arrived",
                table: "mmsi_dispatch_tickets",
                type: "time without time zone",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");

            migrationBuilder.AlterColumn<int>(
                name: "terminal_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "tariff_edited_date",
                table: "mmsi_dispatch_tickets",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<string>(
                name: "tariff_edited_by",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "tariff_by",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "service_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "dispatch_charge_type",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "date_left",
                table: "mmsi_dispatch_tickets",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "date_arrived",
                table: "mmsi_dispatch_tickets",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "date",
                table: "mmsi_dispatch_tickets",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<string>(
                name: "baf_charge_type",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_services_service_id",
                table: "mmsi_dispatch_tickets",
                column: "service_id",
                principalTable: "mmsi_services",
                principalColumn: "service_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_terminals_terminal_id",
                table: "mmsi_dispatch_tickets",
                column: "terminal_id",
                principalTable: "mmsi_terminals",
                principalColumn: "terminal_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_tug_masters_tug_master_id",
                table: "mmsi_dispatch_tickets",
                column: "tug_master_id",
                principalTable: "mmsi_tug_masters",
                principalColumn: "tug_master_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_tugboats_tug_boat_id",
                table: "mmsi_dispatch_tickets",
                column: "tug_boat_id",
                principalTable: "mmsi_tugboats",
                principalColumn: "tugboat_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_vessels_vessel_id",
                table: "mmsi_dispatch_tickets",
                column: "vessel_id",
                principalTable: "mmsi_vessels",
                principalColumn: "vessel_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_services_service_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_terminals_terminal_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_tug_masters_tug_master_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_tugboats_tug_boat_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_vessels_vessel_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.AlterColumn<int>(
                name: "vessel_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "tug_master_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "tug_boat_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "time_left",
                table: "mmsi_dispatch_tickets",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0),
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "time_arrived",
                table: "mmsi_dispatch_tickets",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0),
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "terminal_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "tariff_edited_date",
                table: "mmsi_dispatch_tickets",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "tariff_edited_by",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "tariff_by",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "service_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "dispatch_charge_type",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "date_left",
                table: "mmsi_dispatch_tickets",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "date_arrived",
                table: "mmsi_dispatch_tickets",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "date",
                table: "mmsi_dispatch_tickets",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "baf_charge_type",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_services_service_id",
                table: "mmsi_dispatch_tickets",
                column: "service_id",
                principalTable: "mmsi_services",
                principalColumn: "service_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_terminals_terminal_id",
                table: "mmsi_dispatch_tickets",
                column: "terminal_id",
                principalTable: "mmsi_terminals",
                principalColumn: "terminal_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_tug_masters_tug_master_id",
                table: "mmsi_dispatch_tickets",
                column: "tug_master_id",
                principalTable: "mmsi_tug_masters",
                principalColumn: "tug_master_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_tugboats_tug_boat_id",
                table: "mmsi_dispatch_tickets",
                column: "tug_boat_id",
                principalTable: "mmsi_tugboats",
                principalColumn: "tugboat_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_vessels_vessel_id",
                table: "mmsi_dispatch_tickets",
                column: "vessel_id",
                principalTable: "mmsi_vessels",
                principalColumn: "vessel_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
