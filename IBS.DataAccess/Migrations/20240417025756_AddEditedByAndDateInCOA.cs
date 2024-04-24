using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddEditedByAndDateInCOA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EditedBy",
                table: "ChartOfAccounts",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EditedDate",
                table: "ChartOfAccounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditedBy",
                table: "ChartOfAccounts");

            migrationBuilder.DropColumn(
                name: "EditedDate",
                table: "ChartOfAccounts");
        }
    }
}
