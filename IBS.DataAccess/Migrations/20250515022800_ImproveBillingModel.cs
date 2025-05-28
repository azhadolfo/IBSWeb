using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ImproveBillingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_filpride_customers_customer_id",
                table: "mmsi_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_mmsi_ports_port_id",
                table: "mmsi_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_mmsi_terminals_terminal_id",
                table: "mmsi_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_mmsi_vessels_vessel_id",
                table: "mmsi_billings");

            migrationBuilder.AlterColumn<int>(
                name: "vessel_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "terminal_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "mmsi_billings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "port_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "mmsi_billing_number",
                table: "mmsi_billings",
                type: "varchar(10)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "customer_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_date",
                table: "mmsi_billings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "mmsi_billings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_filpride_customers_customer_id",
                table: "mmsi_billings",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_mmsi_ports_port_id",
                table: "mmsi_billings",
                column: "port_id",
                principalTable: "mmsi_ports",
                principalColumn: "port_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_mmsi_terminals_terminal_id",
                table: "mmsi_billings",
                column: "terminal_id",
                principalTable: "mmsi_terminals",
                principalColumn: "terminal_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_mmsi_vessels_vessel_id",
                table: "mmsi_billings",
                column: "vessel_id",
                principalTable: "mmsi_vessels",
                principalColumn: "vessel_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_filpride_customers_customer_id",
                table: "mmsi_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_mmsi_ports_port_id",
                table: "mmsi_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_mmsi_terminals_terminal_id",
                table: "mmsi_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_mmsi_vessels_vessel_id",
                table: "mmsi_billings");

            migrationBuilder.AlterColumn<int>(
                name: "vessel_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "terminal_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "mmsi_billings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "port_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "mmsi_billing_number",
                table: "mmsi_billings",
                type: "varchar(10)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(10)");

            migrationBuilder.AlterColumn<int>(
                name: "customer_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_date",
                table: "mmsi_billings",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "mmsi_billings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_filpride_customers_customer_id",
                table: "mmsi_billings",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_mmsi_ports_port_id",
                table: "mmsi_billings",
                column: "port_id",
                principalTable: "mmsi_ports",
                principalColumn: "port_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_mmsi_terminals_terminal_id",
                table: "mmsi_billings",
                column: "terminal_id",
                principalTable: "mmsi_terminals",
                principalColumn: "terminal_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_mmsi_vessels_vessel_id",
                table: "mmsi_billings",
                column: "vessel_id",
                principalTable: "mmsi_vessels",
                principalColumn: "vessel_id");
        }
    }
}
