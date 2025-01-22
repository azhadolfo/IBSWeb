using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedFileNameAndSavedUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "proof_of_exemption_file_name",
                table: "filpride_suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "proof_of_registration_file_name",
                table: "filpride_suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supporting_file_saved_file_name",
                table: "filpride_check_voucher_headers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supporting_file_saved_url",
                table: "filpride_check_voucher_headers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "proof_of_exemption_file_name",
                table: "filpride_suppliers");

            migrationBuilder.DropColumn(
                name: "proof_of_registration_file_name",
                table: "filpride_suppliers");

            migrationBuilder.DropColumn(
                name: "supporting_file_saved_file_name",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "supporting_file_saved_url",
                table: "filpride_check_voucher_headers");
        }
    }
}
