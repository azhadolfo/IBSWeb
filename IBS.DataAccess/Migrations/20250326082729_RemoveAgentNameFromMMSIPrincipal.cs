using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAgentNameFromMMSIPrincipal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "agent_name",
                table: "mmsi_principals");

            migrationBuilder.AddColumn<int>(
                name: "customer_id",
                table: "mmsi_principals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_principals_customer_id",
                table: "mmsi_principals",
                column: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_principals_mmsi_customers_customer_id",
                table: "mmsi_principals",
                column: "customer_id",
                principalTable: "mmsi_customers",
                principalColumn: "mmsi_customer_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_principals_mmsi_customers_customer_id",
                table: "mmsi_principals");

            migrationBuilder.DropIndex(
                name: "ix_mmsi_principals_customer_id",
                table: "mmsi_principals");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "mmsi_principals");

            migrationBuilder.AddColumn<string>(
                name: "agent_name",
                table: "mmsi_principals",
                type: "varchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "");
        }
    }
}
