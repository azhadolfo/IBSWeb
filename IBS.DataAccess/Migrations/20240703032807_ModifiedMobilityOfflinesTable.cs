using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedMobilityOfflinesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "opening_dsr_no",
                table: "mobility_offlines",
                newName: "second_dsr_no");

            migrationBuilder.RenameColumn(
                name: "opening",
                table: "mobility_offlines",
                newName: "second_dsr_opening");

            migrationBuilder.RenameColumn(
                name: "closing_dsr_no",
                table: "mobility_offlines",
                newName: "first_dsr_no");

            migrationBuilder.RenameColumn(
                name: "closing",
                table: "mobility_offlines",
                newName: "second_dsr_closing");

            migrationBuilder.AddColumn<decimal>(
                name: "first_dsr_closing",
                table: "mobility_offlines",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "first_dsr_opening",
                table: "mobility_offlines",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "first_dsr_closing",
                table: "mobility_offlines");

            migrationBuilder.DropColumn(
                name: "first_dsr_opening",
                table: "mobility_offlines");

            migrationBuilder.RenameColumn(
                name: "second_dsr_opening",
                table: "mobility_offlines",
                newName: "opening");

            migrationBuilder.RenameColumn(
                name: "second_dsr_no",
                table: "mobility_offlines",
                newName: "opening_dsr_no");

            migrationBuilder.RenameColumn(
                name: "second_dsr_closing",
                table: "mobility_offlines",
                newName: "closing");

            migrationBuilder.RenameColumn(
                name: "first_dsr_no",
                table: "mobility_offlines",
                newName: "closing_dsr_no");
        }
    }
}
