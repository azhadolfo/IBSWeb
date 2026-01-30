using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIsVatableFieldInBillingModel : Migration
    {
        /// <summary>
        /// Adds a non-nullable boolean column named "is_vatable" with a default value of false to the "mmsi_billings" table.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_vatable",
                table: "mmsi_billings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <summary>
        /// Removes the `is_vatable` column from the `mmsi_billings` table.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_vatable",
                table: "mmsi_billings");
        }
    }
}