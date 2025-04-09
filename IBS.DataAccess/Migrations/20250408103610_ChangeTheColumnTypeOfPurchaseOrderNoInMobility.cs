using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTheColumnTypeOfPurchaseOrderNoInMobility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "purchase_order_no",
                table: "mobility_receiving_reports",
                type: "varchar(15)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "purchase_order_no",
                table: "mobility_receiving_reports",
                type: "varchar(12)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(15)",
                oldNullable: true);
        }
    }
}
