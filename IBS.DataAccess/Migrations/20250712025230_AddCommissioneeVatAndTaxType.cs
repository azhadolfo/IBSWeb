using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissioneeVatAndTaxType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "commssionee_tax_type",
                table: "filpride_customer_order_slips",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "commssionee_vat_type",
                table: "filpride_customer_order_slips",
                type: "varchar(20)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "commssionee_tax_type",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "commssionee_vat_type",
                table: "filpride_customer_order_slips");
        }
    }
}
