using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddxTicketIdFieldInFuelAndLubeAndSafeDrop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "x_ticket_id",
                table: "mobility_safe_drops",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "x_ticket_id",
                table: "mobility_lubes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "x_ticket_id",
                table: "mobility_fuels",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_safe_drops_x_ticket_id",
                table: "mobility_safe_drops",
                column: "x_ticket_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lubes_x_ticket_id",
                table: "mobility_lubes",
                column: "x_ticket_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuels_x_ticket_id",
                table: "mobility_fuels",
                column: "x_ticket_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_mobility_safe_drops_x_ticket_id",
                table: "mobility_safe_drops");

            migrationBuilder.DropIndex(
                name: "ix_mobility_lubes_x_ticket_id",
                table: "mobility_lubes");

            migrationBuilder.DropIndex(
                name: "ix_mobility_fuels_x_ticket_id",
                table: "mobility_fuels");

            migrationBuilder.DropColumn(
                name: "x_ticket_id",
                table: "mobility_safe_drops");

            migrationBuilder.DropColumn(
                name: "x_ticket_id",
                table: "mobility_lubes");

            migrationBuilder.DropColumn(
                name: "x_ticket_id",
                table: "mobility_fuels");
        }
    }
}
