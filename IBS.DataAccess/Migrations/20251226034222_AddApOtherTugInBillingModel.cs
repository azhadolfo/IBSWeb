using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddApOtherTugInBillingModel : Migration
    {
        /// <summary>
        /// Adds a non-nullable decimal column "ap_other_tug" (type numeric) to the "mmsi_billings" table with a default value of 0.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ap_other_tug",
                table: "mmsi_billings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ap_other_tug",
                table: "mmsi_billings");
        }
    }
}