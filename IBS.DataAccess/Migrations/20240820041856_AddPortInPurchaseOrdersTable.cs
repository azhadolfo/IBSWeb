using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPortInPurchaseOrdersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "product_no",
                table: "purchase_orders");

            migrationBuilder.RenameColumn(
                name: "supplier_no",
                table: "purchase_orders",
                newName: "port");

            migrationBuilder.AlterColumn<string>(
                name: "terms",
                table: "purchase_orders",
                type: "varchar(5)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(10)");

            migrationBuilder.AlterColumn<decimal>(
                name: "final_price",
                table: "purchase_orders",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "purchase_orders",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "port",
                table: "purchase_orders",
                newName: "supplier_no");

            migrationBuilder.AlterColumn<string>(
                name: "terms",
                table: "purchase_orders",
                type: "varchar(10)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(5)");

            migrationBuilder.AlterColumn<decimal>(
                name: "final_price",
                table: "purchase_orders",
                type: "numeric(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "purchase_orders",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AddColumn<string>(
                name: "product_no",
                table: "purchase_orders",
                type: "text",
                nullable: true);
        }
    }
}
