using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeFieldNameCompanyToStationCodeInMobilityModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "company",
                table: "mobility_suppliers");

            migrationBuilder.AddColumn<string>(
                name: "station_code",
                table: "mobility_suppliers",
                type: "varchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "station_code",
                table: "mobility_suppliers");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "mobility_suppliers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
