using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMMSIModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mmsi_activities_services",
                columns: table => new
                {
                    activity_service_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    activity_service_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    activity_service_name = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_activities_services", x => x.activity_service_id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_company_owners",
                columns: table => new
                {
                    mmsi_company_owner_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_owner_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    comany_owner_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_company_owners", x => x.mmsi_company_owner_id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_customers",
                columns: table => new
                {
                    mmsi_customer_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_name = table.Column<string>(type: "text", nullable: false),
                    customer_address = table.Column<string>(type: "text", nullable: false),
                    customer_tin = table.Column<string>(type: "text", nullable: false),
                    customer_business_style = table.Column<string>(type: "text", nullable: false),
                    customer_terms = table.Column<string>(type: "text", nullable: false),
                    landline1 = table.Column<string>(type: "text", nullable: true),
                    landline2 = table.Column<string>(type: "text", nullable: true),
                    mobile1 = table.Column<string>(type: "text", nullable: true),
                    mobile2 = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: true),
                    is_vatable = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_customers", x => x.mmsi_customer_id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_ports",
                columns: table => new
                {
                    port_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    port_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: true),
                    port_name = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_ports", x => x.port_id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_principals",
                columns: table => new
                {
                    principal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    principal_number = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false),
                    principal_name = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false),
                    agent_name = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    business_type = table.Column<string>(type: "text", nullable: true),
                    terms = table.Column<string>(type: "text", nullable: true),
                    tin = table.Column<string>(type: "text", nullable: true),
                    landline1 = table.Column<string>(type: "text", nullable: true),
                    landline2 = table.Column<string>(type: "text", nullable: true),
                    mobile1 = table.Column<string>(type: "text", nullable: true),
                    mobile2 = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_vatable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_principals", x => x.principal_id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_tug_masters",
                columns: table => new
                {
                    tug_master_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tug_master_number = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false),
                    tug_master_name = table.Column<string>(type: "varchar(30)", maxLength: 25, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_tug_masters", x => x.tug_master_id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_vessels",
                columns: table => new
                {
                    vessel_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vessel_number = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false),
                    vessel_name = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false),
                    vessel_type = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_vessels", x => x.vessel_id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_tugboats",
                columns: table => new
                {
                    tugboat_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tugboat_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    tugboat_name = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false),
                    is_company_owned = table.Column<bool>(type: "boolean", nullable: true),
                    company_owner_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_tugboats", x => x.tugboat_id);
                    table.ForeignKey(
                        name: "fk_mmsi_tugboats_mmsi_company_owners_company_owner_id",
                        column: x => x.company_owner_id,
                        principalTable: "mmsi_company_owners",
                        principalColumn: "mmsi_company_owner_id");
                });

            migrationBuilder.CreateTable(
                name: "mmsi_terminals",
                columns: table => new
                {
                    terminal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    terminal_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: true),
                    terminal_name = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    port_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_terminals", x => x.terminal_id);
                    table.ForeignKey(
                        name: "fk_mmsi_terminals_mmsi_ports_port_id",
                        column: x => x.port_id,
                        principalTable: "mmsi_ports",
                        principalColumn: "port_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_billings",
                columns: table => new
                {
                    mmsi_billing_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    mmsi_billing_number = table.Column<string>(type: "varchar(10)", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "text", nullable: true),
                    is_documented = table.Column<bool>(type: "boolean", nullable: false),
                    voyage_number = table.Column<string>(type: "text", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: true),
                    vessel_id = table.Column<int>(type: "integer", nullable: true),
                    port_id = table.Column<int>(type: "integer", nullable: true),
                    terminal_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_billings", x => x.mmsi_billing_id);
                    table.ForeignKey(
                        name: "fk_mmsi_billings_mmsi_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "mmsi_customers",
                        principalColumn: "mmsi_customer_id");
                    table.ForeignKey(
                        name: "fk_mmsi_billings_mmsi_ports_port_id",
                        column: x => x.port_id,
                        principalTable: "mmsi_ports",
                        principalColumn: "port_id");
                    table.ForeignKey(
                        name: "fk_mmsi_billings_mmsi_terminals_terminal_id",
                        column: x => x.terminal_id,
                        principalTable: "mmsi_terminals",
                        principalColumn: "terminal_id");
                    table.ForeignKey(
                        name: "fk_mmsi_billings_mmsi_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalTable: "mmsi_vessels",
                        principalColumn: "vessel_id");
                });

            migrationBuilder.CreateTable(
                name: "mmsi_dispatch_tickets",
                columns: table => new
                {
                    dispatch_ticket_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    create_date = table.Column<DateOnly>(type: "date", nullable: false),
                    dispatch_number = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    cos_number = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                    date_left = table.Column<DateOnly>(type: "date", nullable: false),
                    date_arrived = table.Column<DateOnly>(type: "date", nullable: false),
                    time_left = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_arrived = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    remarks = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    base_or_station = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    voyage_number = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    dispatch_charge_type = table.Column<string>(type: "text", nullable: true),
                    baf_charge_type = table.Column<string>(type: "text", nullable: true),
                    total_hours = table.Column<decimal>(type: "numeric", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    dispatch_rate = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    dispatch_billing_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    dispatch_discount = table.Column<decimal>(type: "numeric", nullable: true),
                    dispatch_net_revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    baf_rate = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    baf_billing_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    baf_discount = table.Column<decimal>(type: "numeric", nullable: true),
                    baf_net_revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    total_billing = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    total_net_revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    upload_name = table.Column<string>(type: "text", nullable: true),
                    billing_id = table.Column<string>(type: "text", nullable: true),
                    tug_boat_id = table.Column<int>(type: "integer", nullable: false),
                    tug_master_id = table.Column<int>(type: "integer", nullable: false),
                    vessel_id = table.Column<int>(type: "integer", nullable: false),
                    terminal_id = table.Column<int>(type: "integer", nullable: false),
                    activity_service_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_dispatch_tickets", x => x.dispatch_ticket_id);
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_mmsi_activities_services_activity_ser",
                        column: x => x.activity_service_id,
                        principalTable: "mmsi_activities_services",
                        principalColumn: "activity_service_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_mmsi_terminals_terminal_id",
                        column: x => x.terminal_id,
                        principalTable: "mmsi_terminals",
                        principalColumn: "terminal_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_mmsi_tug_masters_tug_master_id",
                        column: x => x.tug_master_id,
                        principalTable: "mmsi_tug_masters",
                        principalColumn: "tug_master_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_mmsi_tugboats_tug_boat_id",
                        column: x => x.tug_boat_id,
                        principalTable: "mmsi_tugboats",
                        principalColumn: "tugboat_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_mmsi_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalTable: "mmsi_vessels",
                        principalColumn: "vessel_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_tariff_rates",
                columns: table => new
                {
                    tariff_rate_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    as_of_date = table.Column<DateOnly>(type: "date", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    terminal_id = table.Column<int>(type: "integer", nullable: false),
                    activity_service_id = table.Column<int>(type: "integer", nullable: false),
                    dispatch = table.Column<decimal>(type: "numeric", nullable: false),
                    baf = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_tariff_rates", x => x.tariff_rate_id);
                    table.ForeignKey(
                        name: "fk_mmsi_tariff_rates_mmsi_activities_services_activity_service",
                        column: x => x.activity_service_id,
                        principalTable: "mmsi_activities_services",
                        principalColumn: "activity_service_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_tariff_rates_mmsi_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "mmsi_customers",
                        principalColumn: "mmsi_customer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_tariff_rates_mmsi_terminals_terminal_id",
                        column: x => x.terminal_id,
                        principalTable: "mmsi_terminals",
                        principalColumn: "terminal_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_billings_customer_id",
                table: "mmsi_billings",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_billings_port_id",
                table: "mmsi_billings",
                column: "port_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_billings_terminal_id",
                table: "mmsi_billings",
                column: "terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_billings_vessel_id",
                table: "mmsi_billings",
                column: "vessel_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_activity_service_id",
                table: "mmsi_dispatch_tickets",
                column: "activity_service_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_terminal_id",
                table: "mmsi_dispatch_tickets",
                column: "terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_tug_boat_id",
                table: "mmsi_dispatch_tickets",
                column: "tug_boat_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_tug_master_id",
                table: "mmsi_dispatch_tickets",
                column: "tug_master_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_vessel_id",
                table: "mmsi_dispatch_tickets",
                column: "vessel_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_tariff_rates_activity_service_id",
                table: "mmsi_tariff_rates",
                column: "activity_service_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_tariff_rates_customer_id",
                table: "mmsi_tariff_rates",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_tariff_rates_terminal_id",
                table: "mmsi_tariff_rates",
                column: "terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_terminals_port_id",
                table: "mmsi_terminals",
                column: "port_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_tugboats_company_owner_id",
                table: "mmsi_tugboats",
                column: "company_owner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mmsi_billings");

            migrationBuilder.DropTable(
                name: "mmsi_dispatch_tickets");

            migrationBuilder.DropTable(
                name: "mmsi_principals");

            migrationBuilder.DropTable(
                name: "mmsi_tariff_rates");

            migrationBuilder.DropTable(
                name: "mmsi_tug_masters");

            migrationBuilder.DropTable(
                name: "mmsi_tugboats");

            migrationBuilder.DropTable(
                name: "mmsi_vessels");

            migrationBuilder.DropTable(
                name: "mmsi_activities_services");

            migrationBuilder.DropTable(
                name: "mmsi_customers");

            migrationBuilder.DropTable(
                name: "mmsi_terminals");

            migrationBuilder.DropTable(
                name: "mmsi_company_owners");

            migrationBuilder.DropTable(
                name: "mmsi_ports");
        }
    }
}
