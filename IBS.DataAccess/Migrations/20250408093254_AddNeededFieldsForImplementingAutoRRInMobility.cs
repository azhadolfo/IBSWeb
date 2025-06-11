using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNeededFieldsForImplementingAutoRRInMobility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                table: "mobility_receiving_reports",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "amount_paid",
                table: "mobility_receiving_reports",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "authority_to_load_no",
                table: "mobility_receiving_reports",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "canceled_quantity",
                table: "mobility_receiving_reports",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "mobility_receiving_reports",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "due_date",
                table: "mobility_receiving_reports",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<decimal>(
                name: "gain_or_loss",
                table: "mobility_receiving_reports",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "is_paid",
                table: "mobility_receiving_reports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_printed",
                table: "mobility_receiving_reports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "paid_date",
                table: "mobility_receiving_reports",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "purchase_order_id",
                table: "mobility_receiving_reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "purchase_order_no",
                table: "mobility_receiving_reports",
                type: "varchar(12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_delivered",
                table: "mobility_receiving_reports",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_received",
                table: "mobility_receiving_reports",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateOnly>(
                name: "received_date",
                table: "mobility_receiving_reports",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "mobility_receiving_reports",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "supplier_dr_no",
                table: "mobility_receiving_reports",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "supplier_invoice_date",
                table: "mobility_receiving_reports",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_invoice_number",
                table: "mobility_receiving_reports",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "truck_or_vessels",
                table: "mobility_receiving_reports",
                type: "varchar(100)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "mobility_receiving_reports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "withdrawal_certificate",
                table: "mobility_receiving_reports",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_receiving_reports_purchase_order_id",
                table: "mobility_receiving_reports",
                column: "purchase_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_receiving_reports_mobility_purchase_orders_purchas",
                table: "mobility_receiving_reports",
                column: "purchase_order_id",
                principalTable: "mobility_purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_receiving_reports_mobility_purchase_orders_purchas",
                table: "mobility_receiving_reports");

            migrationBuilder.DropIndex(
                name: "ix_mobility_receiving_reports_purchase_order_id",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "amount",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "amount_paid",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "authority_to_load_no",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "canceled_quantity",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "company",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "due_date",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "gain_or_loss",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "is_paid",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "is_printed",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "paid_date",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "purchase_order_id",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "purchase_order_no",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "quantity_delivered",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "quantity_received",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "received_date",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "status",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "supplier_dr_no",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "supplier_invoice_date",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "supplier_invoice_number",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "truck_or_vessels",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "type",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "withdrawal_certificate",
                table: "mobility_receiving_reports");
        }
    }
}
