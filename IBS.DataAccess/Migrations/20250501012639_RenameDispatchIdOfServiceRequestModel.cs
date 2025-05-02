using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameDispatchIdOfServiceRequestModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "dispatch_ticket_id",
                table: "mmsi_service_requests",
                newName: "service_request_id");

            migrationBuilder.AddColumn<decimal>(
                name: "total_hours",
                table: "mmsi_service_requests",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "total_hours",
                table: "mmsi_service_requests");

            migrationBuilder.RenameColumn(
                name: "service_request_id",
                table: "mmsi_service_requests",
                newName: "dispatch_ticket_id");
        }
    }
}
