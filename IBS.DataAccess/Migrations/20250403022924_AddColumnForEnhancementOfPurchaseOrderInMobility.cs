using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnForEnhancementOfPurchaseOrderInMobility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "total_amount",
                table: "mobility_purchase_orders",
                newName: "quantity_received");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "mobility_purchase_orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "final_price",
                table: "mobility_purchase_orders",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "is_closed",
                table: "mobility_purchase_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_printed",
                table: "mobility_purchase_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_received",
                table: "mobility_purchase_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "pick_up_point_id",
                table: "mobility_purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_date",
                table: "mobility_purchase_orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "mobility_purchase_orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "supplier_address",
                table: "mobility_purchase_orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "supplier_sales_order_no",
                table: "mobility_purchase_orders",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_tin",
                table: "mobility_purchase_orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "terms",
                table: "mobility_purchase_orders",
                type: "varchar(10)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "mobility_purchase_orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_purchase_orders_pick_up_point_id",
                table: "mobility_purchase_orders",
                column: "pick_up_point_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_purchase_orders_filpride_pick_up_points_pick_up_po",
                table: "mobility_purchase_orders",
                column: "pick_up_point_id",
                principalTable: "filpride_pick_up_points",
                principalColumn: "pick_up_point_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_purchase_orders_filpride_pick_up_points_pick_up_po",
                table: "mobility_purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_mobility_purchase_orders_pick_up_point_id",
                table: "mobility_purchase_orders");

            migrationBuilder.DropColumn(
                name: "company",
                table: "mobility_purchase_orders");

            migrationBuilder.DropColumn(
                name: "final_price",
                table: "mobility_purchase_orders");

            migrationBuilder.DropColumn(
                name: "is_closed",
                table: "mobility_purchase_orders");

            migrationBuilder.DropColumn(
                name: "is_printed",
                table: "mobility_purchase_orders");

            migrationBuilder.DropColumn(
                name: "is_received",
                table: "mobility_purchase_orders");

            migrationBuilder.DropColumn(
                name: "pick_up_point_id",
                table: "mobility_purchase_orders");

            migrationBuilder.DropColumn(
                name: "received_date",
                table: "mobility_purchase_orders");

            migrationBuilder.DropColumn(
                name: "status",
                table: "mobility_purchase_orders");

            migrationBuilder.DropColumn(
                name: "supplier_address",
                table: "mobility_purchase_orders");

            migrationBuilder.DropColumn(
                name: "supplier_sales_order_no",
                table: "mobility_purchase_orders");

            migrationBuilder.DropColumn(
                name: "supplier_tin",
                table: "mobility_purchase_orders");

            migrationBuilder.DropColumn(
                name: "terms",
                table: "mobility_purchase_orders");

            migrationBuilder.DropColumn(
                name: "type",
                table: "mobility_purchase_orders");

            migrationBuilder.RenameColumn(
                name: "quantity_received",
                table: "mobility_purchase_orders",
                newName: "total_amount");
        }
    }
}
