using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeCommissionerNameToCommissionerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "commissioner_name",
                table: "filpride_customer_order_slips");

            migrationBuilder.AddColumn<int>(
                name: "commissioner_id",
                table: "filpride_customer_order_slips",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_commissioner_id",
                table: "filpride_customer_order_slips",
                column: "commissioner_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_suppliers_commission",
                table: "filpride_customer_order_slips",
                column: "commissioner_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_suppliers_commission",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customer_order_slips_commissioner_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "commissioner_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.AddColumn<string>(
                name: "commissioner_name",
                table: "filpride_customer_order_slips",
                type: "varchar(100)",
                nullable: true);
        }
    }
}
