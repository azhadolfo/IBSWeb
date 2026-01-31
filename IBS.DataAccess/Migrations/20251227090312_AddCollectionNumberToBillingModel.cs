using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionNumberToBillingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_mmsi_ports_port_id",
                table: "mmsi_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_mmsi_terminals_terminal_id",
                table: "mmsi_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_mmsi_vessels_vessel_id",
                table: "mmsi_billings");

            migrationBuilder.AlterColumn<int>(
                name: "vessel_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "terminal_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "port_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "collection_number",
                table: "mmsi_billings",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_mmsi_ports_port_id",
                table: "mmsi_billings",
                column: "port_id",
                principalTable: "mmsi_ports",
                principalColumn: "port_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_mmsi_terminals_terminal_id",
                table: "mmsi_billings",
                column: "terminal_id",
                principalTable: "mmsi_terminals",
                principalColumn: "terminal_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_mmsi_vessels_vessel_id",
                table: "mmsi_billings",
                column: "vessel_id",
                principalTable: "mmsi_vessels",
                principalColumn: "vessel_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_mmsi_ports_port_id",
                table: "mmsi_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_mmsi_terminals_terminal_id",
                table: "mmsi_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_mmsi_vessels_vessel_id",
                table: "mmsi_billings");

            migrationBuilder.DropColumn(
                name: "collection_number",
                table: "mmsi_billings");

            migrationBuilder.AlterColumn<int>(
                name: "vessel_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "terminal_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "port_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_mmsi_ports_port_id",
                table: "mmsi_billings",
                column: "port_id",
                principalTable: "mmsi_ports",
                principalColumn: "port_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_mmsi_terminals_terminal_id",
                table: "mmsi_billings",
                column: "terminal_id",
                principalTable: "mmsi_terminals",
                principalColumn: "terminal_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_mmsi_vessels_vessel_id",
                table: "mmsi_billings",
                column: "vessel_id",
                principalTable: "mmsi_vessels",
                principalColumn: "vessel_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
