using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierSnapshotToBookAtlDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "supplier_atl_no",
                table: "filpride_book_atl_details",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "supplier_id",
                table: "filpride_book_atl_details",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_name",
                table: "filpride_book_atl_details",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE filpride_book_atl_details AS bad
                SET
                    supplier_id = src.supplier_id,
                    supplier_name = src.supplier_name,
                    supplier_atl_no = src.supplier_atl_no
                FROM (
                    SELECT
                        bad_inner.id,
                        COALESCE(cas.supplier_id, atl.supplier_id) AS supplier_id,
                        COALESCE(app_supplier.supplier_name, atl_supplier.supplier_name, atl.supplier_name) AS supplier_name,
                        atl.uppi_atl_no AS supplier_atl_no
                    FROM filpride_book_atl_details AS bad_inner
                    LEFT JOIN filpride_cos_appointed_suppliers AS cas
                        ON cas.sequence_id = bad_inner.appointed_id
                    LEFT JOIN filpride_suppliers AS app_supplier
                        ON app_supplier.supplier_id = cas.supplier_id
                    LEFT JOIN filpride_authority_to_loads AS atl
                        ON atl.authority_to_load_id = bad_inner.authority_to_load_id
                    LEFT JOIN filpride_suppliers AS atl_supplier
                        ON atl_supplier.supplier_id = atl.supplier_id
                ) AS src
                WHERE bad.id = src.id;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM filpride_book_atl_details
                        WHERE supplier_id IS NULL
                           OR supplier_name IS NULL
                           OR supplier_name = ''
                    ) THEN
                        RAISE EXCEPTION 'Unable to backfill filpride_book_atl_details supplier snapshot columns. Resolve legacy rows first.';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "supplier_id",
                table: "filpride_book_atl_details",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "supplier_name",
                table: "filpride_book_atl_details",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_book_atl_details_supplier_id",
                table: "filpride_book_atl_details",
                column: "supplier_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_book_atl_details_filpride_suppliers_supplier_id",
                table: "filpride_book_atl_details",
                column: "supplier_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_book_atl_details_filpride_suppliers_supplier_id",
                table: "filpride_book_atl_details");

            migrationBuilder.DropIndex(
                name: "ix_filpride_book_atl_details_supplier_id",
                table: "filpride_book_atl_details");

            migrationBuilder.DropColumn(
                name: "supplier_atl_no",
                table: "filpride_book_atl_details");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "filpride_book_atl_details");

            migrationBuilder.DropColumn(
                name: "supplier_name",
                table: "filpride_book_atl_details");
        }
    }
}
