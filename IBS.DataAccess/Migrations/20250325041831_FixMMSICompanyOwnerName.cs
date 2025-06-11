using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixMMSICompanyOwnerName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "comany_owner_name",
                table: "mmsi_company_owners",
                newName: "company_owner_name");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "mmsi_tug_masters",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "company_owner_name",
                table: "mmsi_company_owners",
                newName: "comany_owner_name");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "mmsi_tug_masters",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }
    }
}
