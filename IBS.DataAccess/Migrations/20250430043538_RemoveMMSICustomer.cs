using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMMSICustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_mmsi_customers_customer_id",
                table: "mmsi_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_collections_mmsi_customers_customer_id",
                table: "mmsi_collections");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_principals_mmsi_customers_customer_id",
                table: "mmsi_principals");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_tariff_rates_mmsi_customers_customer_id",
                table: "mmsi_tariff_rates");

            migrationBuilder.DropTable(
                name: "mmsi_customers");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_filpride_customers_customer_id",
                table: "mmsi_billings",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_collections_filpride_customers_customer_id",
                table: "mmsi_collections",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_principals_filpride_customers_customer_id",
                table: "mmsi_principals",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_tariff_rates_filpride_customers_customer_id",
                table: "mmsi_tariff_rates",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_filpride_customers_customer_id",
                table: "mmsi_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_collections_filpride_customers_customer_id",
                table: "mmsi_collections");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_principals_filpride_customers_customer_id",
                table: "mmsi_principals");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_tariff_rates_filpride_customers_customer_id",
                table: "mmsi_tariff_rates");

            migrationBuilder.CreateTable(
                name: "mmsi_customers",
                columns: table => new
                {
                    mmsi_customer_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_address = table.Column<string>(type: "text", nullable: false),
                    customer_business_style = table.Column<string>(type: "text", nullable: true),
                    customer_name = table.Column<string>(type: "text", nullable: false),
                    customer_number = table.Column<string>(type: "text", nullable: false),
                    customer_tin = table.Column<string>(type: "text", nullable: true),
                    customer_terms = table.Column<string>(type: "text", nullable: true),
                    has_principal = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_vatable = table.Column<bool>(type: "boolean", nullable: false),
                    landline1 = table.Column<string>(type: "text", nullable: true),
                    landline2 = table.Column<string>(type: "text", nullable: true),
                    mobile1 = table.Column<string>(type: "text", nullable: true),
                    mobile2 = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_customers", x => x.mmsi_customer_id);
                });

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_mmsi_customers_customer_id",
                table: "mmsi_billings",
                column: "customer_id",
                principalTable: "mmsi_customers",
                principalColumn: "mmsi_customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_collections_mmsi_customers_customer_id",
                table: "mmsi_collections",
                column: "customer_id",
                principalTable: "mmsi_customers",
                principalColumn: "mmsi_customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_principals_mmsi_customers_customer_id",
                table: "mmsi_principals",
                column: "customer_id",
                principalTable: "mmsi_customers",
                principalColumn: "mmsi_customer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_tariff_rates_mmsi_customers_customer_id",
                table: "mmsi_tariff_rates",
                column: "customer_id",
                principalTable: "mmsi_customers",
                principalColumn: "mmsi_customer_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
