using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateFilprideCOSAppointedSuppliersTableAndAddHasMultiplePOInCOS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_multiple_po",
                table: "filpride_customer_order_slips",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "filpride_cos_appointed_suppliers",
                columns: table => new
                {
                    sequence_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unserved_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_served = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_cos_appointed_suppliers", x => x.sequence_id);
                    table.ForeignKey(
                        name: "fk_filpride_cos_appointed_suppliers_filpride_purchase_orders_p",
                        column: x => x.purchase_order_id,
                        principalTable: "filpride_purchase_orders",
                        principalColumn: "purchase_order_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_cos_appointed_suppliers_customer_order_slip_id",
                table: "filpride_cos_appointed_suppliers",
                column: "customer_order_slip_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_cos_appointed_suppliers_purchase_order_id",
                table: "filpride_cos_appointed_suppliers",
                column: "purchase_order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filpride_cos_appointed_suppliers");

            migrationBuilder.DropColumn(
                name: "has_multiple_po",
                table: "filpride_customer_order_slips");
        }
    }
}
