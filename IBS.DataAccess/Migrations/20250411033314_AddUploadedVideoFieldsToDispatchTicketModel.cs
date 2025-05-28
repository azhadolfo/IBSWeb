using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadedVideoFieldsToDispatchTicketModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "upload_name",
                table: "mmsi_dispatch_tickets",
                newName: "video_signed_url");

            migrationBuilder.RenameColumn(
                name: "signed_url",
                table: "mmsi_dispatch_tickets",
                newName: "video_saved_url");

            migrationBuilder.RenameColumn(
                name: "saved_url",
                table: "mmsi_dispatch_tickets",
                newName: "video_name");

            migrationBuilder.AddColumn<string>(
                name: "image_name",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_saved_url",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_signed_url",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_name",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropColumn(
                name: "image_saved_url",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropColumn(
                name: "image_signed_url",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.RenameColumn(
                name: "video_signed_url",
                table: "mmsi_dispatch_tickets",
                newName: "upload_name");

            migrationBuilder.RenameColumn(
                name: "video_saved_url",
                table: "mmsi_dispatch_tickets",
                newName: "signed_url");

            migrationBuilder.RenameColumn(
                name: "video_name",
                table: "mmsi_dispatch_tickets",
                newName: "saved_url");
        }
    }
}
