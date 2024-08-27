using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusFieldInFilprideCOSTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "has_commision",
                table: "filpride_customer_order_slips",
                newName: "has_commission");

            migrationBuilder.RenameColumn(
                name: "commisioner_name",
                table: "filpride_customer_order_slips",
                newName: "commissioner_name");

            migrationBuilder.RenameColumn(
                name: "commision_rate",
                table: "filpride_customer_order_slips",
                newName: "commission_rate");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "filpride_customer_order_slips",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                table: "filpride_customer_order_slips");

            migrationBuilder.RenameColumn(
                name: "has_commission",
                table: "filpride_customer_order_slips",
                newName: "has_commision");

            migrationBuilder.RenameColumn(
                name: "commissioner_name",
                table: "filpride_customer_order_slips",
                newName: "commisioner_name");

            migrationBuilder.RenameColumn(
                name: "commission_rate",
                table: "filpride_customer_order_slips",
                newName: "commision_rate");
        }
    }
}
