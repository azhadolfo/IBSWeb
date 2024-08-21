using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameTheReceivingReportsToFilprideReceivingReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing foreign key
            migrationBuilder.DropForeignKey(
                name: "fk_receiving_reports_filpride_purchase_orders_po_id",
                table: "receiving_reports"
            );

            // Drop the existing primary key
            migrationBuilder.DropPrimaryKey(
                name: "pk_receiving_reports",
                table: "receiving_reports"
            );

            // Rename the table
            migrationBuilder.RenameTable(
                name: "receiving_reports",
                newName: "filpride_receiving_reports"
            );

            // Rename the index
            migrationBuilder.RenameIndex(
                name: "ix_receiving_reports_po_id",
                table: "filpride_receiving_reports",
                newName: "ix_filpride_receiving_reports_po_id"
            );

            // Add the new primary key
            migrationBuilder.AddPrimaryKey(
                name: "pk_filpride_receiving_reports",
                table: "filpride_receiving_reports",
                column: "receiving_report_id"
            );

            // Add the new foreign key
            migrationBuilder.AddForeignKey(
                name: "fk_filpride_receiving_reports_filpride_purchase_orders_po_id",
                table: "filpride_receiving_reports",
                column: "po_id",
                principalTable: "filpride_purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the foreign key
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_receiving_reports_filpride_purchase_orders_po_id",
                table: "filpride_receiving_reports"
            );

            // Drop the primary key
            migrationBuilder.DropPrimaryKey(
                name: "pk_filpride_receiving_reports",
                table: "filpride_receiving_reports"
            );

            // Rename the table back
            migrationBuilder.RenameTable(
                name: "filpride_receiving_reports",
                newName: "receiving_reports"
            );

            // Rename the index back
            migrationBuilder.RenameIndex(
                name: "ix_filpride_receiving_reports_po_id",
                table: "receiving_reports",
                newName: "ix_receiving_reports_po_id"
            );

            // Add the old primary key back
            migrationBuilder.AddPrimaryKey(
                name: "pk_receiving_reports",
                table: "receiving_reports",
                column: "receiving_report_id"
            );

            // Add the old foreign key back
            migrationBuilder.AddForeignKey(
                name: "fk_receiving_reports_filpride_purchase_orders_po_id",
                table: "receiving_reports",
                column: "po_id",
                principalTable: "filpride_purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
