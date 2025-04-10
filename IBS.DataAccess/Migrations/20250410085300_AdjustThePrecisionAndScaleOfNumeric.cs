using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AdjustThePrecisionAndScaleOfNumeric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "trust_fee_rate",
                table: "bienes_placements",
                type: "numeric(11,8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(3,8)");

            migrationBuilder.AlterColumn<decimal>(
                name: "interest_rate",
                table: "bienes_placements",
                type: "numeric(13,10)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(3,10)");

            migrationBuilder.AlterColumn<string>(
                name: "interest_deposited_to",
                table: "bienes_placements",
                type: "varchar(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "frequency_of_payment",
                table: "bienes_placements",
                type: "varchar(20)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ewt_rate",
                table: "bienes_placements",
                type: "numeric(7,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(3,4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "trust_fee_rate",
                table: "bienes_placements",
                type: "numeric(3,8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(11,8)");

            migrationBuilder.AlterColumn<decimal>(
                name: "interest_rate",
                table: "bienes_placements",
                type: "numeric(3,10)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(13,10)");

            migrationBuilder.AlterColumn<string>(
                name: "interest_deposited_to",
                table: "bienes_placements",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "frequency_of_payment",
                table: "bienes_placements",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ewt_rate",
                table: "bienes_placements",
                type: "numeric(3,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(7,4)");
        }
    }
}
