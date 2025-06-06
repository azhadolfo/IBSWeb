using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRequestModelAndRenameDispatchDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "create_date",
                table: "mmsi_dispatch_tickets",
                newName: "date");

            migrationBuilder.CreateTable(
                name: "mmsi_service_requests",
                columns: table => new
                {
                    dispatch_ticket_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    dispatch_number = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    cos_number = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                    date_left = table.Column<DateOnly>(type: "date", nullable: false),
                    date_arrived = table.Column<DateOnly>(type: "date", nullable: false),
                    time_left = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_arrived = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    remarks = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    base_or_station = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    voyage_number = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    edited_by = table.Column<string>(type: "text", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    image_name = table.Column<string>(type: "text", nullable: true),
                    image_saved_url = table.Column<string>(type: "text", nullable: true),
                    image_signed_url = table.Column<string>(type: "text", nullable: true),
                    video_name = table.Column<string>(type: "text", nullable: true),
                    video_saved_url = table.Column<string>(type: "text", nullable: true),
                    video_signed_url = table.Column<string>(type: "text", nullable: true),
                    billing_id = table.Column<string>(type: "text", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: true),
                    tug_boat_id = table.Column<int>(type: "integer", nullable: false),
                    tug_master_id = table.Column<int>(type: "integer", nullable: false),
                    vessel_id = table.Column<int>(type: "integer", nullable: false),
                    terminal_id = table.Column<int>(type: "integer", nullable: false),
                    activity_service_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_service_requests", x => x.dispatch_ticket_id);
                    table.ForeignKey(
                        name: "fk_mmsi_service_requests_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id");
                    table.ForeignKey(
                        name: "fk_mmsi_service_requests_mmsi_activities_services_activity_ser",
                        column: x => x.activity_service_id,
                        principalTable: "mmsi_activities_services",
                        principalColumn: "activity_service_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_service_requests_mmsi_terminals_terminal_id",
                        column: x => x.terminal_id,
                        principalTable: "mmsi_terminals",
                        principalColumn: "terminal_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_service_requests_mmsi_tug_masters_tug_master_id",
                        column: x => x.tug_master_id,
                        principalTable: "mmsi_tug_masters",
                        principalColumn: "tug_master_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_service_requests_mmsi_tugboats_tug_boat_id",
                        column: x => x.tug_boat_id,
                        principalTable: "mmsi_tugboats",
                        principalColumn: "tugboat_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_service_requests_mmsi_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalTable: "mmsi_vessels",
                        principalColumn: "vessel_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_service_requests_activity_service_id",
                table: "mmsi_service_requests",
                column: "activity_service_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_service_requests_customer_id",
                table: "mmsi_service_requests",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_service_requests_terminal_id",
                table: "mmsi_service_requests",
                column: "terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_service_requests_tug_boat_id",
                table: "mmsi_service_requests",
                column: "tug_boat_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_service_requests_tug_master_id",
                table: "mmsi_service_requests",
                column: "tug_master_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_service_requests_vessel_id",
                table: "mmsi_service_requests",
                column: "vessel_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mmsi_service_requests");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "mmsi_dispatch_tickets",
                newName: "create_date");
        }
    }
}
