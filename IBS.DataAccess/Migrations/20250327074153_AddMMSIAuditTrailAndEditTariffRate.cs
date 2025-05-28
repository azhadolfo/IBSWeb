using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMMSIAuditTrailAndEditTariffRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "mmsi_tariff_rates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_date",
                table: "mmsi_tariff_rates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "mmsi_audit_trails",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    machine_name = table.Column<string>(type: "text", nullable: false),
                    activity = table.Column<string>(type: "text", nullable: false),
                    document_type = table.Column<string>(type: "text", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_audit_trails", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mmsi_audit_trails");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "mmsi_tariff_rates");

            migrationBuilder.DropColumn(
                name: "created_date",
                table: "mmsi_tariff_rates");
        }
    }
}
