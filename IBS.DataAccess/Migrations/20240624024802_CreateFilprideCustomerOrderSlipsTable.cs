using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateFilprideCustomerOrderSlipsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filpride_customer_order_slips",
                columns: table => new
                {
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_order_slip_no = table.Column<string>(type: "varchar(13)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    remarks = table.Column<string>(type: "varchar(200)", nullable: false),
                    po_no = table.Column<string>(type: "varchar(100)", nullable: false),
                    delivery_date_and_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    vat = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    delivered_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    delivered_quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    balance_quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_delivered = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    edited_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    approved_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    disapproved_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    disapproved_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_customer_order_slips", x => x.customer_order_slip_id);
                    table.ForeignKey(
                        name: "fk_filpride_customer_order_slips_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_customer_order_slips_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_customer_id",
                table: "filpride_customer_order_slips",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_customer_order_slip_no",
                table: "filpride_customer_order_slips",
                column: "customer_order_slip_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_date",
                table: "filpride_customer_order_slips",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_product_id",
                table: "filpride_customer_order_slips",
                column: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filpride_customer_order_slips");
        }
    }
}
