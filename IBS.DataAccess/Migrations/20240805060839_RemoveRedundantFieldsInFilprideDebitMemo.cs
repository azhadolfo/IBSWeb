using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantFieldsInFilprideDebitMemo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dm_no",
                table: "filpride_debit_memos");

            migrationBuilder.DropColumn(
                name: "series_number",
                table: "filpride_debit_memos");

            migrationBuilder.AlterColumn<string>(
                name: "service_no",
                table: "filpride_services",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "service_no",
                table: "filpride_services",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dm_no",
                table: "filpride_debit_memos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "series_number",
                table: "filpride_debit_memos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
