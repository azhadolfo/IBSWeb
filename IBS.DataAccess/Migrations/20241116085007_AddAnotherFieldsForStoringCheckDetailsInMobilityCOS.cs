using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAnotherFieldsForStoringCheckDetailsInMobilityCOS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_check_details_required",
                table: "mobility_customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "check_no",
                table: "mobility_customer_order_slips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "check_picture_saved_file_name",
                table: "mobility_customer_order_slips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "check_picture_saved_url",
                table: "mobility_customer_order_slips",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_check_details_required",
                table: "mobility_customers");

            migrationBuilder.DropColumn(
                name: "check_no",
                table: "mobility_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "check_picture_saved_file_name",
                table: "mobility_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "check_picture_saved_url",
                table: "mobility_customer_order_slips");
        }
    }
}
