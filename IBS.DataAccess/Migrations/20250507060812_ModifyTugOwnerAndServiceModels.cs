using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ModifyTugOwnerAndServiceModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_activities_services_activity_ser",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_tariff_rates_mmsi_activities_services_activity_service",
                table: "mmsi_tariff_rates");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_tugboats_mmsi_company_owners_company_owner_id",
                table: "mmsi_tugboats");

            migrationBuilder.DropColumn(
                name: "activity_service_name",
                table: "mmsi_activities_services");

            migrationBuilder.RenameColumn(
                name: "company_owner_id",
                table: "mmsi_tugboats",
                newName: "tugboat_owner_id");

            migrationBuilder.RenameIndex(
                name: "ix_mmsi_tugboats_company_owner_id",
                table: "mmsi_tugboats",
                newName: "ix_mmsi_tugboats_tugboat_owner_id");

            migrationBuilder.RenameColumn(
                name: "activity_service_id",
                table: "mmsi_tariff_rates",
                newName: "service_id");

            migrationBuilder.RenameIndex(
                name: "ix_mmsi_tariff_rates_activity_service_id",
                table: "mmsi_tariff_rates",
                newName: "ix_mmsi_tariff_rates_service_id");

            migrationBuilder.RenameColumn(
                name: "activity_service_id",
                table: "mmsi_dispatch_tickets",
                newName: "service_id");

            migrationBuilder.RenameIndex(
                name: "ix_mmsi_dispatch_tickets_activity_service_id",
                table: "mmsi_dispatch_tickets",
                newName: "ix_mmsi_dispatch_tickets_service_id");

            migrationBuilder.RenameColumn(
                name: "company_owner_number",
                table: "mmsi_company_owners",
                newName: "tugboat_owner_number");

            migrationBuilder.RenameColumn(
                name: "company_owner_name",
                table: "mmsi_company_owners",
                newName: "tugboat_owner_name");

            migrationBuilder.RenameColumn(
                name: "mmsi_company_owner_id",
                table: "mmsi_company_owners",
                newName: "tugboat_owner_id");

            migrationBuilder.RenameColumn(
                name: "activity_service_number",
                table: "mmsi_activities_services",
                newName: "service_number");

            migrationBuilder.RenameColumn(
                name: "activity_service_id",
                table: "mmsi_activities_services",
                newName: "service_id");

            migrationBuilder.AddColumn<string>(
                name: "service_name",
                table: "mmsi_activities_services",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "service_name",
                table: "mmsi_activities_services");

            migrationBuilder.RenameColumn(
                name: "tugboat_owner_id",
                table: "mmsi_tugboats",
                newName: "company_owner_id");

            migrationBuilder.RenameIndex(
                name: "ix_mmsi_tugboats_tugboat_owner_id",
                table: "mmsi_tugboats",
                newName: "ix_mmsi_tugboats_company_owner_id");

            migrationBuilder.RenameColumn(
                name: "service_id",
                table: "mmsi_tariff_rates",
                newName: "activity_service_id");

            migrationBuilder.RenameIndex(
                name: "ix_mmsi_tariff_rates_service_id",
                table: "mmsi_tariff_rates",
                newName: "ix_mmsi_tariff_rates_activity_service_id");

            migrationBuilder.RenameColumn(
                name: "service_id",
                table: "mmsi_dispatch_tickets",
                newName: "activity_service_id");

            migrationBuilder.RenameIndex(
                name: "ix_mmsi_dispatch_tickets_service_id",
                table: "mmsi_dispatch_tickets",
                newName: "ix_mmsi_dispatch_tickets_activity_service_id");

            migrationBuilder.RenameColumn(
                name: "tugboat_owner_number",
                table: "mmsi_company_owners",
                newName: "company_owner_number");

            migrationBuilder.RenameColumn(
                name: "tugboat_owner_name",
                table: "mmsi_company_owners",
                newName: "company_owner_name");

            migrationBuilder.RenameColumn(
                name: "tugboat_owner_id",
                table: "mmsi_company_owners",
                newName: "mmsi_company_owner_id");

            migrationBuilder.RenameColumn(
                name: "service_number",
                table: "mmsi_activities_services",
                newName: "activity_service_number");

            migrationBuilder.RenameColumn(
                name: "service_id",
                table: "mmsi_activities_services",
                newName: "activity_service_id");

            migrationBuilder.AddColumn<string>(
                name: "activity_service_name",
                table: "mmsi_activities_services",
                type: "varchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_activities_services_activity_ser",
                table: "mmsi_dispatch_tickets",
                column: "activity_service_id",
                principalTable: "mmsi_activities_services",
                principalColumn: "activity_service_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_tariff_rates_mmsi_activities_services_activity_service",
                table: "mmsi_tariff_rates",
                column: "activity_service_id",
                principalTable: "mmsi_activities_services",
                principalColumn: "activity_service_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_tugboats_mmsi_company_owners_company_owner_id",
                table: "mmsi_tugboats",
                column: "company_owner_id",
                principalTable: "mmsi_company_owners",
                principalColumn: "mmsi_company_owner_id");
        }
    }
}
