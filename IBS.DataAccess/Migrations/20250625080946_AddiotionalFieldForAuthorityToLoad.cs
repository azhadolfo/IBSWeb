using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddiotionalFieldForAuthorityToLoad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "company",
                table: "filpride_authority_to_loads",
                type: "varchar(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "driver",
                table: "filpride_authority_to_loads",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hauler_name",
                table: "filpride_authority_to_loads",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "load_port",
                table: "filpride_authority_to_loads",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "plate_no",
                table: "filpride_authority_to_loads",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_name",
                table: "filpride_authority_to_loads",
                type: "varchar(100)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "driver",
                table: "filpride_authority_to_loads");

            migrationBuilder.DropColumn(
                name: "hauler_name",
                table: "filpride_authority_to_loads");

            migrationBuilder.DropColumn(
                name: "load_port",
                table: "filpride_authority_to_loads");

            migrationBuilder.DropColumn(
                name: "plate_no",
                table: "filpride_authority_to_loads");

            migrationBuilder.DropColumn(
                name: "supplier_name",
                table: "filpride_authority_to_loads");

            migrationBuilder.AlterColumn<string>(
                name: "company",
                table: "filpride_authority_to_loads",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)");
        }
    }
}
