using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedURLAndSignedURLMMSIDispatchCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "saved_url",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "signed_url",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "saved_url",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropColumn(
                name: "signed_url",
                table: "mmsi_dispatch_tickets");
        }
    }
}
