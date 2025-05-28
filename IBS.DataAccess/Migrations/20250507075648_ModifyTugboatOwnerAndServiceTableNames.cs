using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ModifyTugboatOwnerAndServiceTableNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_activities_services_service_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_tariff_rates_mmsi_activities_services_service_id",
                table: "mmsi_tariff_rates");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_tugboats_mmsi_company_owners_tugboat_owner_id",
                table: "mmsi_tugboats");

            migrationBuilder.DropPrimaryKey(
                name: "pk_mmsi_company_owners",
                table: "mmsi_company_owners");

            migrationBuilder.DropPrimaryKey(
                name: "pk_mmsi_activities_services",
                table: "mmsi_activities_services");

            migrationBuilder.RenameTable(
                name: "mmsi_company_owners",
                newName: "mmsi_tugboat_owners");

            migrationBuilder.RenameTable(
                name: "mmsi_activities_services",
                newName: "mmsi_services");

            migrationBuilder.AddPrimaryKey(
                name: "pk_mmsi_tugboat_owners",
                table: "mmsi_tugboat_owners",
                column: "tugboat_owner_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_mmsi_services",
                table: "mmsi_services",
                column: "service_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_services_service_id",
                table: "mmsi_dispatch_tickets",
                column: "service_id",
                principalTable: "mmsi_services",
                principalColumn: "service_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_tariff_rates_mmsi_services_service_id",
                table: "mmsi_tariff_rates",
                column: "service_id",
                principalTable: "mmsi_services",
                principalColumn: "service_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_tugboats_mmsi_tugboat_owners_tugboat_owner_id",
                table: "mmsi_tugboats",
                column: "tugboat_owner_id",
                principalTable: "mmsi_tugboat_owners",
                principalColumn: "tugboat_owner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_services_service_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_tariff_rates_mmsi_services_service_id",
                table: "mmsi_tariff_rates");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_tugboats_mmsi_tugboat_owners_tugboat_owner_id",
                table: "mmsi_tugboats");

            migrationBuilder.DropPrimaryKey(
                name: "pk_mmsi_tugboat_owners",
                table: "mmsi_tugboat_owners");

            migrationBuilder.DropPrimaryKey(
                name: "pk_mmsi_services",
                table: "mmsi_services");

            migrationBuilder.RenameTable(
                name: "mmsi_tugboat_owners",
                newName: "mmsi_company_owners");

            migrationBuilder.RenameTable(
                name: "mmsi_services",
                newName: "mmsi_activities_services");

            migrationBuilder.AddPrimaryKey(
                name: "pk_mmsi_company_owners",
                table: "mmsi_company_owners",
                column: "tugboat_owner_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_mmsi_activities_services",
                table: "mmsi_activities_services",
                column: "service_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_activities_services_service_id",
                table: "mmsi_dispatch_tickets",
                column: "service_id",
                principalTable: "mmsi_activities_services",
                principalColumn: "service_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_tariff_rates_mmsi_activities_services_service_id",
                table: "mmsi_tariff_rates",
                column: "service_id",
                principalTable: "mmsi_activities_services",
                principalColumn: "service_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_tugboats_mmsi_company_owners_tugboat_owner_id",
                table: "mmsi_tugboats",
                column: "tugboat_owner_id",
                principalTable: "mmsi_company_owners",
                principalColumn: "tugboat_owner_id");
        }
    }
}
