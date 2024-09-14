using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyOfCustomerOrderSlipOnTheAuthorityToLoadTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_authority_to_loads_a",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customer_order_slips_authority_to_load_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "authority_to_load_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.AddColumn<string>(
                name: "authority_to_load_no",
                table: "filpride_customer_order_slips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "customer_order_slip_id",
                table: "filpride_authority_to_loads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_authority_to_loads_customer_order_slip_id",
                table: "filpride_authority_to_loads",
                column: "customer_order_slip_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_authority_to_loads_filpride_customer_order_slips_c",
                table: "filpride_authority_to_loads",
                column: "customer_order_slip_id",
                principalTable: "filpride_customer_order_slips",
                principalColumn: "customer_order_slip_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_authority_to_loads_filpride_customer_order_slips_c",
                table: "filpride_authority_to_loads");

            migrationBuilder.DropIndex(
                name: "ix_filpride_authority_to_loads_customer_order_slip_id",
                table: "filpride_authority_to_loads");

            migrationBuilder.DropColumn(
                name: "authority_to_load_no",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "customer_order_slip_id",
                table: "filpride_authority_to_loads");

            migrationBuilder.AddColumn<int>(
                name: "authority_to_load_id",
                table: "filpride_customer_order_slips",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_authority_to_load_id",
                table: "filpride_customer_order_slips",
                column: "authority_to_load_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_authority_to_loads_a",
                table: "filpride_customer_order_slips",
                column: "authority_to_load_id",
                principalTable: "filpride_authority_to_loads",
                principalColumn: "authority_to_load_id");
        }
    }
}
