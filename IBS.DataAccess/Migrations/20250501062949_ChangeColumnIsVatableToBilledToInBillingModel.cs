using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeColumnIsVatableToBilledToInBillingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_vatable",
                table: "mmsi_billings");

            migrationBuilder.AddColumn<string>(
                name: "billed_to",
                table: "mmsi_billings",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "billed_to",
                table: "mmsi_billings");

            migrationBuilder.AddColumn<bool>(
                name: "is_vatable",
                table: "mmsi_billings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
