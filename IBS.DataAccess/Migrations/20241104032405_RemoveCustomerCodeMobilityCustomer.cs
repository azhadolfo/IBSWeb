using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCustomerCodeMobilityCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_mobility_customers_customer_code",
                table: "mobility_customers");

            migrationBuilder.DropColumn(
                name: "customer_code",
                table: "mobility_customers");

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "mobility_customer_order_slips",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customers_customer_id",
                table: "mobility_customers",
                column: "customer_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_mobility_customers_customer_id",
                table: "mobility_customers");

            migrationBuilder.AddColumn<string>(
                name: "customer_code",
                table: "mobility_customers",
                type: "varchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "mobility_customer_order_slips",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customers_customer_code",
                table: "mobility_customers",
                column: "customer_code",
                unique: true);
        }
    }
}
