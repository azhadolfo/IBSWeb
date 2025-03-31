using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPrincipalRelevantFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_principal",
                table: "mmsi_customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_principal",
                table: "mmsi_billings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "principal_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_billings_principal_id",
                table: "mmsi_billings",
                column: "principal_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_mmsi_principals_principal_id",
                table: "mmsi_billings",
                column: "principal_id",
                principalTable: "mmsi_principals",
                principalColumn: "principal_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_mmsi_principals_principal_id",
                table: "mmsi_billings");

            migrationBuilder.DropIndex(
                name: "ix_mmsi_billings_principal_id",
                table: "mmsi_billings");

            migrationBuilder.DropColumn(
                name: "has_principal",
                table: "mmsi_customers");

            migrationBuilder.DropColumn(
                name: "is_principal",
                table: "mmsi_billings");

            migrationBuilder.DropColumn(
                name: "principal_id",
                table: "mmsi_billings");
        }
    }
}
