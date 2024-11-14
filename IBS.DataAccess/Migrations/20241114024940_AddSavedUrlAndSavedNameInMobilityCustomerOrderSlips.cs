using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedUrlAndSavedNameInMobilityCustomerOrderSlips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "upload",
                table: "mobility_customer_order_slips");

            migrationBuilder.AddColumn<string>(
                name: "saved_file_name",
                table: "mobility_customer_order_slips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "saved_url",
                table: "mobility_customer_order_slips",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "saved_file_name",
                table: "mobility_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "saved_url",
                table: "mobility_customer_order_slips");

            migrationBuilder.AddColumn<string>(
                name: "upload",
                table: "mobility_customer_order_slips",
                type: "varchar(1024)",
                nullable: true);
        }
    }
}
