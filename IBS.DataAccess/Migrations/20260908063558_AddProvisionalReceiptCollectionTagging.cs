using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddProvisionalReceiptCollectionTagging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_provisional_receipts_filpride_suppliers_supplier_id",
                table: "filpride_provisional_receipts");

            migrationBuilder.RenameColumn(
                name: "supplier_id",
                table: "filpride_provisional_receipts",
                newName: "tagged_supplier_id");

            migrationBuilder.RenameIndex(
                name: "ix_filpride_provisional_receipts_supplier_id",
                table: "filpride_provisional_receipts",
                newName: "ix_filpride_provisional_receipts_tagged_supplier_id");

            migrationBuilder.AlterColumn<int>(
                name: "tagged_supplier_id",
                table: "filpride_provisional_receipts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "collection_category_id",
                table: "filpride_provisional_receipts",
                type: "integer",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "payer_address",
                table: "filpride_provisional_receipts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payer_name",
                table: "filpride_provisional_receipts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "tag_type",
                table: "filpride_provisional_receipts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tagged_bank_account_id",
                table: "filpride_provisional_receipts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tagged_company_id",
                table: "filpride_provisional_receipts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "filpride_collection_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tagging_requirement = table.Column<int>(type: "integer", nullable: false),
                    allow_company = table.Column<bool>(type: "boolean", nullable: false),
                    allow_employee = table.Column<bool>(type: "boolean", nullable: false),
                    allow_bank_account = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    edited_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_collection_categories", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_provisional_receipts_collection_category_id",
                table: "filpride_provisional_receipts",
                column: "collection_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_provisional_receipts_tagged_bank_account_id",
                table: "filpride_provisional_receipts",
                column: "tagged_bank_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_provisional_receipts_tagged_company_id",
                table: "filpride_provisional_receipts",
                column: "tagged_company_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_collection_categories_name",
                table: "filpride_collection_categories",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_provisional_receipts_companies_tagged_company_id",
                table: "filpride_provisional_receipts",
                column: "tagged_company_id",
                principalTable: "companies",
                principalColumn: "company_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_provisional_receipts_filpride_bank_accounts_tagged",
                table: "filpride_provisional_receipts",
                column: "tagged_bank_account_id",
                principalTable: "filpride_bank_accounts",
                principalColumn: "bank_account_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_provisional_receipts_filpride_collection_categorie",
                table: "filpride_provisional_receipts",
                column: "collection_category_id",
                principalTable: "filpride_collection_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_provisional_receipts_filpride_suppliers_tagged_sup",
                table: "filpride_provisional_receipts",
                column: "tagged_supplier_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_provisional_receipts_companies_tagged_company_id",
                table: "filpride_provisional_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_provisional_receipts_filpride_bank_accounts_tagged",
                table: "filpride_provisional_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_provisional_receipts_filpride_collection_categorie",
                table: "filpride_provisional_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_provisional_receipts_filpride_suppliers_tagged_sup",
                table: "filpride_provisional_receipts");

            migrationBuilder.DropTable(
                name: "filpride_collection_categories");

            migrationBuilder.DropIndex(
                name: "ix_filpride_provisional_receipts_collection_category_id",
                table: "filpride_provisional_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_provisional_receipts_tagged_bank_account_id",
                table: "filpride_provisional_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_provisional_receipts_tagged_company_id",
                table: "filpride_provisional_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_provisional_receipts_tagged_supplier_id",
                table: "filpride_provisional_receipts");

            migrationBuilder.DropColumn(
                name: "payer_address",
                table: "filpride_provisional_receipts");

            migrationBuilder.DropColumn(
                name: "payer_name",
                table: "filpride_provisional_receipts");

            migrationBuilder.DropColumn(
                name: "tag_type",
                table: "filpride_provisional_receipts");

            migrationBuilder.DropColumn(
                name: "tagged_bank_account_id",
                table: "filpride_provisional_receipts");

            migrationBuilder.DropColumn(
                name: "tagged_company_id",
                table: "filpride_provisional_receipts");

            migrationBuilder.RenameColumn(
                name: "tagged_supplier_id",
                table: "filpride_provisional_receipts",
                newName: "supplier_id");

            migrationBuilder.RenameIndex(
                name: "ix_filpride_provisional_receipts_tagged_supplier_id",
                table: "filpride_provisional_receipts",
                newName: "ix_filpride_provisional_receipts_supplier_id");

            migrationBuilder.AlterColumn<int>(
                name: "supplier_id",
                table: "filpride_provisional_receipts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_provisional_receipts_filpride_suppliers_supplier_id",
                table: "filpride_provisional_receipts",
                column: "supplier_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
