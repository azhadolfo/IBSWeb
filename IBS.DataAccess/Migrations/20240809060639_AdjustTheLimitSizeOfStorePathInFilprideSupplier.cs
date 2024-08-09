using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AdjustTheLimitSizeOfStorePathInFilprideSupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "proof_of_registration_file_path",
                table: "filpride_suppliers",
                type: "varchar(1024)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "proof_of_exemption_file_path",
                table: "filpride_suppliers",
                type: "varchar(1024)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "proof_of_registration_file_path",
                table: "filpride_suppliers",
                type: "varchar(200)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1024)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "proof_of_exemption_file_path",
                table: "filpride_suppliers",
                type: "varchar(200)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1024)",
                oldNullable: true);
        }
    }
}
