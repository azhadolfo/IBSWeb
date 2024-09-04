using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCommisionerDetailsInFilprideCustomerOrderSlipsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "commision_rate",
                table: "filpride_customer_order_slips",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "commisioner_name",
                table: "filpride_customer_order_slips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_commision",
                table: "filpride_customer_order_slips",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "commision_rate",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "commisioner_name",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "has_commision",
                table: "filpride_customer_order_slips");
        }
    }
}
