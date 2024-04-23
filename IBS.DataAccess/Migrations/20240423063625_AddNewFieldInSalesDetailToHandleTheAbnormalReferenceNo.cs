using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFieldInSalesDetailToHandleTheAbnormalReferenceNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesHeaders_StationPosCode",
                table: "SalesHeaders");

            migrationBuilder.AlterColumn<string>(
                name: "SalesNo",
                table: "SalesHeaders",
                type: "varchar(15)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "SalesNo",
                table: "SalesDetails",
                type: "varchar(15)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNo",
                table: "SalesDetails",
                type: "varchar(15)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SalesHeaders_StationCode",
                table: "SalesHeaders",
                column: "StationCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesHeaders_StationCode",
                table: "SalesHeaders");

            migrationBuilder.DropColumn(
                name: "ReferenceNo",
                table: "SalesDetails");

            migrationBuilder.AlterColumn<string>(
                name: "SalesNo",
                table: "SalesHeaders",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(15)");

            migrationBuilder.AlterColumn<string>(
                name: "SalesNo",
                table: "SalesDetails",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(15)");

            migrationBuilder.CreateIndex(
                name: "IX_SalesHeaders_StationPosCode",
                table: "SalesHeaders",
                column: "StationPosCode");
        }
    }
}
