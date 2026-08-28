using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCompanyFromAuthorityToLoad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_filpride_authority_to_loads_authority_to_load_no_company",
                table: "filpride_authority_to_loads");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_authority_to_loads");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_authority_to_loads_authority_to_load_no",
                table: "filpride_authority_to_loads",
                column: "authority_to_load_no",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_filpride_authority_to_loads_authority_to_load_no",
                table: "filpride_authority_to_loads");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_authority_to_loads",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_authority_to_loads_authority_to_load_no_company",
                table: "filpride_authority_to_loads",
                columns: new[] { "authority_to_load_no", "company" },
                unique: true);
        }
    }
}
