using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeTableInMobilityStation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mobility_station_employees",
                columns: table => new
                {
                    employee_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_number = table.Column<string>(type: "varchar(10)", nullable: false),
                    initial = table.Column<string>(type: "text", nullable: true),
                    first_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    middle_name = table.Column<string>(type: "varchar(100)", nullable: true),
                    last_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    suffix = table.Column<string>(type: "varchar(5)", nullable: true),
                    address = table.Column<string>(type: "varchar(255)", nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    tel_no = table.Column<string>(type: "text", nullable: true),
                    sss_no = table.Column<string>(type: "text", nullable: true),
                    tin_no = table.Column<string>(type: "varchar(20)", nullable: true),
                    philhealth_no = table.Column<string>(type: "text", nullable: true),
                    pagibig_no = table.Column<string>(type: "text", nullable: true),
                    station_code = table.Column<string>(type: "text", nullable: true),
                    department = table.Column<string>(type: "text", nullable: true),
                    date_hired = table.Column<DateOnly>(type: "date", nullable: false),
                    date_resigned = table.Column<DateOnly>(type: "date", nullable: true),
                    position = table.Column<string>(type: "text", nullable: false),
                    is_managerial = table.Column<bool>(type: "boolean", nullable: false),
                    supervisor = table.Column<string>(type: "varchar(20)", nullable: false),
                    status = table.Column<string>(type: "varchar(5)", nullable: false),
                    paygrade = table.Column<string>(type: "text", nullable: true),
                    salary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_station_employees", x => x.employee_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mobility_station_employees");
        }
    }
}
