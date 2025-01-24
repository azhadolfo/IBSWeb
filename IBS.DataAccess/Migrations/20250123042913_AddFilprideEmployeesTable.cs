using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFilprideEmployeesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filpride_employees",
                columns: table => new
                {
                    employee_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_number = table.Column<string>(type: "varchar(10)", nullable: false),
                    initial = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    middle_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    last_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    suffix = table.Column<string>(type: "varchar(5)", nullable: false),
                    address = table.Column<string>(type: "varchar(255)", nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    tel_no = table.Column<string>(type: "varchar(20)", nullable: false),
                    sss_no = table.Column<string>(type: "varchar(20)", nullable: false),
                    tin_no = table.Column<string>(type: "varchar(20)", nullable: false),
                    philhealth_no = table.Column<string>(type: "varchar(20)", nullable: false),
                    pagibig_no = table.Column<string>(type: "varchar(20)", nullable: false),
                    company = table.Column<string>(type: "varchar(20)", nullable: false),
                    depatment = table.Column<string>(type: "varchar(20)", nullable: false),
                    date_hired = table.Column<DateOnly>(type: "date", nullable: false),
                    date_resigned = table.Column<DateOnly>(type: "date", nullable: true),
                    position = table.Column<string>(type: "varchar(20)", nullable: false),
                    is_managerial = table.Column<bool>(type: "boolean", nullable: false),
                    supervisor = table.Column<string>(type: "varchar(20)", nullable: false),
                    status = table.Column<string>(type: "varchar(5)", nullable: false),
                    paygrade = table.Column<string>(type: "text", nullable: false),
                    salary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_employees", x => x.employee_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_employees_employee_number",
                table: "filpride_employees",
                column: "employee_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filpride_employees");
        }
    }
}
