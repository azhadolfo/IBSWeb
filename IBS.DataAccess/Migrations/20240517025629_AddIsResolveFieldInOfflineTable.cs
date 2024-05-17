using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIsResolveFieldInOfflineTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsResolve",
                table: "Offlines",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsResolve",
                table: "Offlines");
        }
    }
}
