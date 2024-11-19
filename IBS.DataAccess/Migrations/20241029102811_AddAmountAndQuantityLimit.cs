using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAmountAndQuantityLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "amount_limit",
                table: "mobility_customers",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_limit",
                table: "mobility_customers",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "amount_limit",
                table: "mobility_customers");

            migrationBuilder.DropColumn(
                name: "customer_address",
                table: "mobility_customers");

            migrationBuilder.DropColumn(
                name: "quantity_limit",
                table: "mobility_customers");
        }
    }
}
