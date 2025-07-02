using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameTheApprover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "first_approved_date",
                table: "filpride_customer_order_slips",
                newName: "om_approved_date");

            migrationBuilder.RenameColumn(
                name: "first_approved_by",
                table: "filpride_customer_order_slips",
                newName: "om_approved_by");

            migrationBuilder.RenameColumn(
                name: "second_approved_date",
                table: "filpride_customer_order_slips",
                newName: "fm_approved_date");

            migrationBuilder.RenameColumn(
                name: "second_approved_by",
                table: "filpride_customer_order_slips",
                newName: "fm_approved_by");

            migrationBuilder.RenameColumn(
                name: "operation_manager_reason",
                table: "filpride_customer_order_slips",
                newName: "om_reason");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "om_reason",
                table: "filpride_customer_order_slips",
                newName: "operation_manager_reason");

            migrationBuilder.RenameColumn(
                name: "om_approved_date",
                table: "filpride_customer_order_slips",
                newName: "second_approved_date");

            migrationBuilder.RenameColumn(
                name: "om_approved_by",
                table: "filpride_customer_order_slips",
                newName: "second_approved_by");

            migrationBuilder.RenameColumn(
                name: "fm_approved_date",
                table: "filpride_customer_order_slips",
                newName: "first_approved_date");

            migrationBuilder.RenameColumn(
                name: "fm_approved_by",
                table: "filpride_customer_order_slips",
                newName: "first_approved_by");
        }
    }
}
