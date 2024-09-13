using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameTheCommissionerToCommissionee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "commissioner_id",
                table: "filpride_customer_order_slips",
                newName: "commissionee_id");

            migrationBuilder.RenameIndex(
                name: "ix_filpride_customer_order_slips_commissioner_id",
                table: "filpride_customer_order_slips",
                newName: "ix_filpride_customer_order_slips_commissionee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "commissionee_id",
                table: "filpride_customer_order_slips",
                newName: "commissioner_id");

            migrationBuilder.RenameIndex(
                name: "ix_filpride_customer_order_slips_commissionee_id",
                table: "filpride_customer_order_slips",
                newName: "ix_filpride_customer_order_slips_commissioner_id");
        }
    }
}
