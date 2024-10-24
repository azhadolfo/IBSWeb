using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMobilityCustomerOrderSlipTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mobility_customer_order_slips",
                columns: table => new
                {
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_order_slip_no = table.Column<string>(type: "varchar(13)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price_per_liter = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    plate_no = table.Column<string>(type: "text", nullable: false),
                    driver = table.Column<string>(type: "text", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    upload = table.Column<string>(type: "varchar(1024)", nullable: true),
                    load_date = table.Column<DateOnly>(type: "date", nullable: true),
                    station_code = table.Column<string>(type: "text", nullable: false),
                    terms = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    disapproved_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    disapproved_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_customer_order_slips", x => x.customer_order_slip_id);
                    table.ForeignKey(
                        name: "fk_mobility_customer_order_slips_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mobility_customer_order_slips_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_order_slips_customer_id",
                table: "mobility_customer_order_slips",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_order_slips_product_id",
                table: "mobility_customer_order_slips",
                column: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mobility_customer_order_slips");
        }
    }
}
