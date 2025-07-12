using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CorrectTheCommissioneeTypeInCos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "commssionee_vat_type",
                table: "filpride_customer_order_slips",
                newName: "commissionee_vat_type");

            migrationBuilder.RenameColumn(
                name: "commssionee_tax_type",
                table: "filpride_customer_order_slips",
                newName: "commissionee_tax_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "commissionee_vat_type",
                table: "filpride_customer_order_slips",
                newName: "commssionee_vat_type");

            migrationBuilder.RenameColumn(
                name: "commissionee_tax_type",
                table: "filpride_customer_order_slips",
                newName: "commssionee_tax_type");
        }
    }
}
