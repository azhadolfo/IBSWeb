using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverAndPLateNoOnFilprideCOSTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "driver",
                table: "filpride_customer_order_slips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plate_no",
                table: "filpride_customer_order_slips",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "driver",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "plate_no",
                table: "filpride_customer_order_slips");
        }
    }
}
