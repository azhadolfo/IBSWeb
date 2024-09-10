using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTheNullablityTypeOfCommissionerRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "commission_rate",
                table: "filpride_customer_order_slips",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "commission_rate",
                table: "filpride_customer_order_slips",
                type: "numeric(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");
        }
    }
}
