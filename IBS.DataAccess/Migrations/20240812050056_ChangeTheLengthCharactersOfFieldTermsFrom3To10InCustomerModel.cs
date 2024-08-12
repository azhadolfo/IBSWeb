using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTheLengthCharactersOfFieldTermsFrom3To10InCustomerModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "customer_terms",
                table: "filpride_customers",
                type: "varchar(10)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "customer_terms",
                table: "filpride_customers",
                type: "varchar(3)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(10)");
        }
    }
}
