using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMobilityFMSFuelSalesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mobility_cos_appointed_suppliers");

            migrationBuilder.DropTable(
                name: "mobility_main_cos");

            migrationBuilder.CreateTable(
                name: "mobility_fms_fuel_sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_code = table.Column<string>(type: "text", nullable: false),
                    shift_record_id = table.Column<string>(type: "text", nullable: false),
                    pump_number = table.Column<int>(type: "integer", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    opening = table.Column<decimal>(type: "numeric", nullable: false),
                    closing = table.Column<decimal>(type: "numeric", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_number = table.Column<int>(type: "integer", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_fms_fuel_sales", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fms_fuel_sales_shift_record_id",
                table: "mobility_fms_fuel_sales",
                column: "shift_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fms_fuel_sales_station_code",
                table: "mobility_fms_fuel_sales",
                column: "station_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mobility_fms_fuel_sales");

            migrationBuilder.CreateTable(
                name: "mobility_main_cos",
                columns: table => new
                {
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    commissionee_id = table.Column<int>(type: "integer", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    hauler_id = table.Column<int>(type: "integer", nullable: true),
                    pick_up_point_id = table.Column<int>(type: "integer", nullable: true),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: true),
                    supplier_id = table.Column<int>(type: "integer", nullable: true),
                    account_specialist = table.Column<string>(type: "varchar(100)", nullable: false),
                    authority_to_load_no = table.Column<string>(type: "varchar(20)", nullable: true),
                    balance_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    branch = table.Column<string>(type: "text", nullable: true),
                    commission_rate = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    customer_address = table.Column<string>(type: "text", nullable: false),
                    customer_order_slip_no = table.Column<string>(type: "varchar(13)", nullable: false),
                    customer_po_no = table.Column<string>(type: "varchar(100)", nullable: false),
                    customer_tin = table.Column<string>(type: "text", nullable: false),
                    customer_type = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    delivered_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    delivered_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    delivery_option = table.Column<string>(type: "varchar(50)", nullable: true),
                    disapproved_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    disapproved_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    driver = table.Column<string>(type: "text", nullable: true),
                    edited_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expiration_date = table.Column<DateOnly>(type: "date", nullable: true),
                    finance_instruction = table.Column<string>(type: "text", nullable: true),
                    first_approved_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    first_approved_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    freight = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    has_commission = table.Column<bool>(type: "boolean", nullable: false),
                    has_multiple_po = table.Column<bool>(type: "boolean", nullable: false),
                    is_delivered = table.Column<bool>(type: "boolean", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    old_cos_no = table.Column<string>(type: "text", nullable: false),
                    operation_manager_reason = table.Column<string>(type: "text", nullable: true),
                    plate_no = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    remarks = table.Column<string>(type: "text", nullable: false),
                    second_approved_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    second_approved_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    sub_po_remarks = table.Column<string>(type: "text", nullable: true),
                    terms = table.Column<string>(type: "text", nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_main_cos", x => x.customer_order_slip_id);
                    table.ForeignKey(
                        name: "fk_mobility_main_cos_mobility_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "mobility_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mobility_main_cos_mobility_pick_up_points_pick_up_point_id",
                        column: x => x.pick_up_point_id,
                        principalTable: "mobility_pick_up_points",
                        principalColumn: "pick_up_point_id");
                    table.ForeignKey(
                        name: "fk_mobility_main_cos_mobility_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "mobility_purchase_orders",
                        principalColumn: "purchase_order_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mobility_main_cos_mobility_suppliers_commissionee_id",
                        column: x => x.commissionee_id,
                        principalTable: "mobility_suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mobility_main_cos_mobility_suppliers_hauler_id",
                        column: x => x.hauler_id,
                        principalTable: "mobility_suppliers",
                        principalColumn: "supplier_id");
                    table.ForeignKey(
                        name: "fk_mobility_main_cos_mobility_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "mobility_suppliers",
                        principalColumn: "supplier_id");
                    table.ForeignKey(
                        name: "fk_mobility_main_cos_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mobility_cos_appointed_suppliers",
                columns: table => new
                {
                    sequence_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    atl_no = table.Column<string>(type: "text", nullable: true),
                    is_assigned_to_dr = table.Column<bool>(type: "boolean", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unserved_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_cos_appointed_suppliers", x => x.sequence_id);
                    table.ForeignKey(
                        name: "fk_mobility_cos_appointed_suppliers_mobility_main_cos_customer",
                        column: x => x.customer_order_slip_id,
                        principalTable: "mobility_main_cos",
                        principalColumn: "customer_order_slip_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mobility_cos_appointed_suppliers_mobility_purchase_orders_p",
                        column: x => x.purchase_order_id,
                        principalTable: "mobility_purchase_orders",
                        principalColumn: "purchase_order_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mobility_cos_appointed_suppliers_mobility_suppliers_supplie",
                        column: x => x.supplier_id,
                        principalTable: "mobility_suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mobility_cos_appointed_suppliers_customer_order_slip_id",
                table: "mobility_cos_appointed_suppliers",
                column: "customer_order_slip_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_cos_appointed_suppliers_purchase_order_id",
                table: "mobility_cos_appointed_suppliers",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_cos_appointed_suppliers_supplier_id",
                table: "mobility_cos_appointed_suppliers",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_main_cos_commissionee_id",
                table: "mobility_main_cos",
                column: "commissionee_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_main_cos_customer_id",
                table: "mobility_main_cos",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_main_cos_customer_order_slip_no",
                table: "mobility_main_cos",
                column: "customer_order_slip_no");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_main_cos_date",
                table: "mobility_main_cos",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_main_cos_hauler_id",
                table: "mobility_main_cos",
                column: "hauler_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_main_cos_pick_up_point_id",
                table: "mobility_main_cos",
                column: "pick_up_point_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_main_cos_product_id",
                table: "mobility_main_cos",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_main_cos_purchase_order_id",
                table: "mobility_main_cos",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_main_cos_supplier_id",
                table: "mobility_main_cos",
                column: "supplier_id");
        }
    }
}
