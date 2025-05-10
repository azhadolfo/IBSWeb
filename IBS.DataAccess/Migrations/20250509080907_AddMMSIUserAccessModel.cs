using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMMSIUserAccessModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mmsi_user_accesses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    user_name = table.Column<string>(type: "varchar(100)", nullable: true),
                    can_create_service_request = table.Column<bool>(type: "boolean", nullable: false),
                    can_post_service_request = table.Column<bool>(type: "boolean", nullable: false),
                    can_create_dispatch_ticket = table.Column<bool>(type: "boolean", nullable: false),
                    can_set_tariff = table.Column<bool>(type: "boolean", nullable: false),
                    can_approve_tariff = table.Column<bool>(type: "boolean", nullable: false),
                    can_create_billing = table.Column<bool>(type: "boolean", nullable: false),
                    can_create_collection = table.Column<bool>(type: "boolean", nullable: false),
                    can_print_report = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_user_accesses", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mmsi_user_accesses");
        }
    }
}
