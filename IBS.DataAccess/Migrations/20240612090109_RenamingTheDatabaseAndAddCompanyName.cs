using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenamingTheDatabaseAndAddCompanyName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fuel_deliveries");

            migrationBuilder.DropTable(
                name: "fuel_purchase");

            migrationBuilder.DropTable(
                name: "fuels");

            migrationBuilder.DropTable(
                name: "lube_deliveries");

            migrationBuilder.DropTable(
                name: "lube_purchase_details");

            migrationBuilder.DropTable(
                name: "lubes");

            migrationBuilder.DropTable(
                name: "offlines");

            migrationBuilder.DropTable(
                name: "po_sales");

            migrationBuilder.DropTable(
                name: "po_sales_raw");

            migrationBuilder.DropTable(
                name: "safe_drops");

            migrationBuilder.DropTable(
                name: "sales_details");

            migrationBuilder.DropTable(
                name: "lube_purchase_headers");

            migrationBuilder.DropTable(
                name: "sales_headers");

            migrationBuilder.CreateTable(
                name: "mobility_fuel_deliveries",
                columns: table => new
                {
                    fuel_delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shiftrecid = table.Column<string>(type: "varchar(20)", nullable: false),
                    stncode = table.Column<string>(type: "varchar(5)", nullable: false),
                    cashiercode = table.Column<string>(type: "text", nullable: false),
                    shiftnumber = table.Column<int>(type: "integer", nullable: false),
                    deliverydate = table.Column<DateOnly>(type: "date", nullable: false),
                    timein = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    timeout = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    driver = table.Column<string>(type: "varchar(100)", nullable: false),
                    hauler = table.Column<string>(type: "varchar(100)", nullable: false),
                    platenumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    drnumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    wcnumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    tanknumber = table.Column<int>(type: "integer", nullable: false),
                    productcode = table.Column<string>(type: "varchar(10)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    purchaseprice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    sellprice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    volumebefore = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    volumeafter = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    receivedby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createdby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createddate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_fuel_deliveries", x => x.fuel_delivery_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_fuel_purchase",
                columns: table => new
                {
                    fuel_purchase_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fuel_purchase_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    shift_rec_id = table.Column<string>(type: "varchar(20)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    cashier_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    shift_no = table.Column<int>(type: "integer", nullable: false),
                    delivery_date = table.Column<DateOnly>(type: "date", nullable: false),
                    time_in = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_out = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    driver = table.Column<string>(type: "varchar(100)", nullable: false),
                    hauler = table.Column<string>(type: "varchar(100)", nullable: false),
                    plate_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    dr_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    wc_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    tank_no = table.Column<int>(type: "integer", nullable: false),
                    product_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    purchase_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    selling_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    quantity_before = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    quantity_after = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    should_be = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    gain_or_loss = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    received_by = table.Column<string>(type: "varchar(50)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_fuel_purchase", x => x.fuel_purchase_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_fuels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    start = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    inv_date = table.Column<DateOnly>(type: "date", nullable: false),
                    x_corpcode = table.Column<int>(type: "integer", nullable: false),
                    x_sitecode = table.Column<int>(type: "integer", nullable: false),
                    x_tank = table.Column<int>(type: "integer", nullable: false),
                    x_pump = table.Column<int>(type: "integer", nullable: false),
                    x_nozzle = table.Column<int>(type: "integer", nullable: false),
                    x_year = table.Column<int>(type: "integer", nullable: false),
                    x_month = table.Column<int>(type: "integer", nullable: false),
                    x_day = table.Column<int>(type: "integer", nullable: false),
                    x_transaction = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    amount_db = table.Column<decimal>(type: "numeric", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    calibration = table.Column<decimal>(type: "numeric", nullable: false),
                    volume = table.Column<decimal>(type: "numeric", nullable: false),
                    item_code = table.Column<string>(type: "varchar(16)", nullable: false),
                    particulars = table.Column<string>(type: "varchar(32)", nullable: false),
                    opening = table.Column<decimal>(type: "numeric", nullable: false),
                    closing = table.Column<decimal>(type: "numeric", nullable: false),
                    nozdown = table.Column<string>(type: "varchar(20)", nullable: false),
                    in_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    out_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    liters = table.Column<decimal>(type: "numeric", nullable: false),
                    x_oid = table.Column<string>(type: "varchar(20)", nullable: false),
                    x_oname = table.Column<string>(type: "varchar(20)", nullable: false),
                    shift = table.Column<int>(type: "integer", nullable: false),
                    plateno = table.Column<string>(type: "varchar(20)", nullable: true),
                    pono = table.Column<string>(type: "varchar(20)", nullable: true),
                    cust = table.Column<string>(type: "varchar(20)", nullable: true),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    detail_group = table.Column<int>(type: "integer", nullable: false),
                    trans_count = table.Column<int>(type: "integer", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_fuels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_lube_deliveries",
                columns: table => new
                {
                    lube_delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shiftrecid = table.Column<string>(type: "varchar(20)", nullable: false),
                    stncode = table.Column<string>(type: "varchar(5)", nullable: false),
                    cashiercode = table.Column<string>(type: "text", nullable: false),
                    shiftnumber = table.Column<int>(type: "integer", nullable: false),
                    deliverydate = table.Column<DateOnly>(type: "date", nullable: false),
                    suppliercode = table.Column<string>(type: "varchar(10)", nullable: false),
                    invoiceno = table.Column<string>(type: "varchar(50)", nullable: false),
                    drno = table.Column<string>(type: "varchar(50)", nullable: false),
                    pono = table.Column<string>(type: "varchar(50)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    rcvdby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createdby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createddate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    dtllink = table.Column<string>(type: "varchar(50)", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit = table.Column<string>(type: "varchar(10)", nullable: false),
                    description = table.Column<string>(type: "varchar(200)", nullable: false),
                    unitprice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    productcode = table.Column<string>(type: "varchar(10)", nullable: false),
                    piece = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_lube_deliveries", x => x.lube_delivery_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_lube_purchase_headers",
                columns: table => new
                {
                    lube_purchase_header_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lube_purchase_header_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    shift_rec_id = table.Column<string>(type: "varchar(20)", nullable: false),
                    detail_link = table.Column<string>(type: "varchar(50)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    cashier_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    shift_no = table.Column<int>(type: "integer", nullable: false),
                    delivery_date = table.Column<DateOnly>(type: "date", nullable: false),
                    sales_invoice = table.Column<string>(type: "varchar(50)", nullable: false),
                    supplier_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    dr_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    po_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    vatable_sales = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    received_by = table.Column<string>(type: "varchar(50)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_lube_purchase_headers", x => x.lube_purchase_header_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_lubes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inv_date = table.Column<DateOnly>(type: "date", nullable: false),
                    x_year = table.Column<int>(type: "integer", nullable: false),
                    x_month = table.Column<int>(type: "integer", nullable: false),
                    x_day = table.Column<int>(type: "integer", nullable: false),
                    x_corpcode = table.Column<int>(type: "integer", nullable: false),
                    x_sitecode = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    amount_db = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    lubes_qty = table.Column<decimal>(type: "numeric", nullable: false),
                    item_code = table.Column<string>(type: "varchar(16)", nullable: false),
                    particulars = table.Column<string>(type: "varchar(100)", nullable: false),
                    x_oid = table.Column<string>(type: "varchar(10)", nullable: false),
                    cashier = table.Column<string>(type: "varchar(20)", nullable: false),
                    shift = table.Column<int>(type: "integer", nullable: false),
                    x_transaction = table.Column<long>(type: "bigint", nullable: false),
                    x_stamp = table.Column<string>(type: "varchar(50)", nullable: false),
                    plateno = table.Column<string>(type: "varchar(20)", nullable: true),
                    pono = table.Column<string>(type: "varchar(20)", nullable: true),
                    cust = table.Column<string>(type: "varchar(20)", nullable: true),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_lubes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_offlines",
                columns: table => new
                {
                    offline_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    series_no = table.Column<int>(type: "integer", nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    product = table.Column<string>(type: "varchar(20)", nullable: false),
                    pump = table.Column<int>(type: "integer", nullable: false),
                    opening = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    closing = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    liters = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    closing_dsr_no = table.Column<string>(type: "text", nullable: false),
                    opening_dsr_no = table.Column<string>(type: "text", nullable: false),
                    is_resolve = table.Column<bool>(type: "boolean", nullable: false),
                    new_closing = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_offlines", x => x.offline_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_po_sales",
                columns: table => new
                {
                    po_sales_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    po_sales_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    shift_rec_id = table.Column<string>(type: "varchar(20)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    cashier_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    shift_no = table.Column<int>(type: "integer", nullable: false),
                    po_sales_date = table.Column<DateOnly>(type: "date", nullable: false),
                    po_sales_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    customer_code = table.Column<string>(type: "varchar(20)", nullable: false),
                    driver = table.Column<string>(type: "varchar(50)", nullable: false),
                    plate_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    dr_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    trip_ticket = table.Column<string>(type: "varchar(20)", nullable: false),
                    product_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    contract_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_po_sales", x => x.po_sales_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_po_sales_raw",
                columns: table => new
                {
                    po_sales_raw_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shiftrecid = table.Column<string>(type: "varchar(20)", nullable: false),
                    stncode = table.Column<string>(type: "varchar(5)", nullable: false),
                    cashiercode = table.Column<string>(type: "text", nullable: false),
                    shiftnumber = table.Column<int>(type: "integer", nullable: false),
                    podate = table.Column<DateOnly>(type: "date", nullable: false),
                    potime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    customercode = table.Column<string>(type: "varchar(20)", nullable: false),
                    driver = table.Column<string>(type: "varchar(50)", nullable: false),
                    plateno = table.Column<string>(type: "varchar(50)", nullable: false),
                    drnumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    tripticket = table.Column<string>(type: "text", nullable: false),
                    productcode = table.Column<string>(type: "varchar(10)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    contractprice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    createdby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createddate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_po_sales_raw", x => x.po_sales_raw_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_safe_drops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inv_date = table.Column<DateOnly>(type: "date", nullable: false),
                    b_date = table.Column<DateOnly>(type: "date", nullable: false),
                    x_year = table.Column<int>(type: "integer", nullable: false),
                    x_month = table.Column<int>(type: "integer", nullable: false),
                    x_day = table.Column<int>(type: "integer", nullable: false),
                    x_corpcode = table.Column<int>(type: "integer", nullable: false),
                    x_sitecode = table.Column<int>(type: "integer", nullable: false),
                    t_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    x_stamp = table.Column<string>(type: "varchar(50)", nullable: false),
                    x_oid = table.Column<string>(type: "varchar(10)", nullable: false),
                    x_oname = table.Column<string>(type: "varchar(20)", nullable: false),
                    shift = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_safe_drops", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_sales_headers",
                columns: table => new
                {
                    sales_header_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sales_no = table.Column<string>(type: "varchar(15)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    cashier = table.Column<string>(type: "varchar(20)", nullable: false),
                    shift = table.Column<int>(type: "integer", nullable: false),
                    time_in = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_out = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    fuel_sales_total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    lubes_total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    safe_drop_total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    po_sales_amount = table.Column<decimal[]>(type: "numeric(18,2)[]", nullable: false),
                    customers = table.Column<string[]>(type: "varchar[]", nullable: false),
                    po_sales_total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_sales = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    gain_or_loss = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_transaction_normal = table.Column<bool>(type: "boolean", nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false),
                    source = table.Column<string>(type: "varchar(10)", nullable: false),
                    actual_cash_on_hand = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    particular = table.Column<string>(type: "varchar(200)", nullable: true),
                    is_modified = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_sales_headers", x => x.sales_header_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_lube_purchase_details",
                columns: table => new
                {
                    lube_purchase_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lube_purchase_header_id = table.Column<int>(type: "integer", nullable: false),
                    lube_purchase_header_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit = table.Column<string>(type: "varchar(10)", nullable: false),
                    description = table.Column<string>(type: "varchar(200)", nullable: false),
                    cost_per_case = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    cost_per_piece = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    product_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    piece = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_lube_purchase_details", x => x.lube_purchase_detail_id);
                    table.ForeignKey(
                        name: "fk_mobility_lube_purchase_details_mobility_lube_purchase_heade",
                        column: x => x.lube_purchase_header_id,
                        principalTable: "mobility_lube_purchase_headers",
                        principalColumn: "lube_purchase_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mobility_sales_details",
                columns: table => new
                {
                    sales_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sales_header_id = table.Column<int>(type: "integer", nullable: false),
                    sales_no = table.Column<string>(type: "varchar(15)", nullable: false),
                    product = table.Column<string>(type: "varchar(20)", nullable: false),
                    particular = table.Column<string>(type: "varchar(50)", nullable: false),
                    closing = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    opening = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    liters = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    calibration = table.Column<decimal>(type: "numeric", nullable: false),
                    liters_sold = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    transaction_count = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    sale = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    reference_no = table.Column<string>(type: "varchar(15)", nullable: true),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_sales_details", x => x.sales_detail_id);
                    table.ForeignKey(
                        name: "fk_mobility_sales_details_mobility_sales_headers_sales_header_",
                        column: x => x.sales_header_id,
                        principalTable: "mobility_sales_headers",
                        principalColumn: "sales_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuel_deliveries_shiftrecid",
                table: "mobility_fuel_deliveries",
                column: "shiftrecid");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuel_deliveries_stncode",
                table: "mobility_fuel_deliveries",
                column: "stncode");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuel_purchase_fuel_purchase_no",
                table: "mobility_fuel_purchase",
                column: "fuel_purchase_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuel_purchase_product_code",
                table: "mobility_fuel_purchase",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuel_purchase_station_code",
                table: "mobility_fuel_purchase",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuels_inv_date",
                table: "mobility_fuels",
                column: "inv_date");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuels_is_processed",
                table: "mobility_fuels",
                column: "is_processed");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuels_item_code",
                table: "mobility_fuels",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuels_particulars",
                table: "mobility_fuels",
                column: "particulars");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuels_shift",
                table: "mobility_fuels",
                column: "shift");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuels_x_oname",
                table: "mobility_fuels",
                column: "x_oname");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuels_x_pump",
                table: "mobility_fuels",
                column: "x_pump");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuels_x_sitecode",
                table: "mobility_fuels",
                column: "x_sitecode");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_deliveries_dtllink",
                table: "mobility_lube_deliveries",
                column: "dtllink");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_deliveries_shiftrecid",
                table: "mobility_lube_deliveries",
                column: "shiftrecid");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_deliveries_stncode",
                table: "mobility_lube_deliveries",
                column: "stncode");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_purchase_details_lube_purchase_header_id",
                table: "mobility_lube_purchase_details",
                column: "lube_purchase_header_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_purchase_details_lube_purchase_header_no",
                table: "mobility_lube_purchase_details",
                column: "lube_purchase_header_no");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_purchase_details_product_code",
                table: "mobility_lube_purchase_details",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_purchase_headers_lube_purchase_header_no",
                table: "mobility_lube_purchase_headers",
                column: "lube_purchase_header_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_purchase_headers_station_code",
                table: "mobility_lube_purchase_headers",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lubes_cashier",
                table: "mobility_lubes",
                column: "cashier");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lubes_inv_date",
                table: "mobility_lubes",
                column: "inv_date");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_po_sales_po_sales_no",
                table: "mobility_po_sales",
                column: "po_sales_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_po_sales_raw_shiftrecid",
                table: "mobility_po_sales_raw",
                column: "shiftrecid");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_po_sales_raw_stncode",
                table: "mobility_po_sales_raw",
                column: "stncode");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_po_sales_raw_tripticket",
                table: "mobility_po_sales_raw",
                column: "tripticket");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_safe_drops_inv_date",
                table: "mobility_safe_drops",
                column: "inv_date");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_safe_drops_x_oname",
                table: "mobility_safe_drops",
                column: "x_oname");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_sales_details_sales_header_id",
                table: "mobility_sales_details",
                column: "sales_header_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_sales_details_sales_no",
                table: "mobility_sales_details",
                column: "sales_no");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_sales_details_station_code",
                table: "mobility_sales_details",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_sales_headers_cashier",
                table: "mobility_sales_headers",
                column: "cashier");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_sales_headers_date",
                table: "mobility_sales_headers",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_sales_headers_sales_no",
                table: "mobility_sales_headers",
                column: "sales_no");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_sales_headers_shift",
                table: "mobility_sales_headers",
                column: "shift");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_sales_headers_station_code",
                table: "mobility_sales_headers",
                column: "station_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mobility_fuel_deliveries");

            migrationBuilder.DropTable(
                name: "mobility_fuel_purchase");

            migrationBuilder.DropTable(
                name: "mobility_fuels");

            migrationBuilder.DropTable(
                name: "mobility_lube_deliveries");

            migrationBuilder.DropTable(
                name: "mobility_lube_purchase_details");

            migrationBuilder.DropTable(
                name: "mobility_lubes");

            migrationBuilder.DropTable(
                name: "mobility_offlines");

            migrationBuilder.DropTable(
                name: "mobility_po_sales");

            migrationBuilder.DropTable(
                name: "mobility_po_sales_raw");

            migrationBuilder.DropTable(
                name: "mobility_safe_drops");

            migrationBuilder.DropTable(
                name: "mobility_sales_details");

            migrationBuilder.DropTable(
                name: "mobility_lube_purchase_headers");

            migrationBuilder.DropTable(
                name: "mobility_sales_headers");

            migrationBuilder.CreateTable(
                name: "fuel_deliveries",
                columns: table => new
                {
                    fuel_delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cashiercode = table.Column<string>(type: "text", nullable: false),
                    createdby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createddate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    deliverydate = table.Column<DateOnly>(type: "date", nullable: false),
                    driver = table.Column<string>(type: "varchar(100)", nullable: false),
                    drnumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    hauler = table.Column<string>(type: "varchar(100)", nullable: false),
                    platenumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    productcode = table.Column<string>(type: "varchar(10)", nullable: false),
                    purchaseprice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    receivedby = table.Column<string>(type: "varchar(50)", nullable: false),
                    sellprice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    shiftnumber = table.Column<int>(type: "integer", nullable: false),
                    shiftrecid = table.Column<string>(type: "varchar(20)", nullable: false),
                    stncode = table.Column<string>(type: "varchar(5)", nullable: false),
                    tanknumber = table.Column<int>(type: "integer", nullable: false),
                    timein = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    timeout = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    volumeafter = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    volumebefore = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    wcnumber = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fuel_deliveries", x => x.fuel_delivery_id);
                });

            migrationBuilder.CreateTable(
                name: "fuel_purchase",
                columns: table => new
                {
                    fuel_purchase_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cashier_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    delivery_date = table.Column<DateOnly>(type: "date", nullable: false),
                    dr_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    driver = table.Column<string>(type: "varchar(100)", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fuel_purchase_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    gain_or_loss = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    hauler = table.Column<string>(type: "varchar(100)", nullable: false),
                    plate_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    product_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    purchase_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    quantity_after = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    quantity_before = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    received_by = table.Column<string>(type: "varchar(50)", nullable: false),
                    selling_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    shift_no = table.Column<int>(type: "integer", nullable: false),
                    shift_rec_id = table.Column<string>(type: "varchar(20)", nullable: false),
                    should_be = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    tank_no = table.Column<int>(type: "integer", nullable: false),
                    time_in = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_out = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    wc_no = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fuel_purchase", x => x.fuel_purchase_id);
                });

            migrationBuilder.CreateTable(
                name: "fuels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    amount_db = table.Column<decimal>(type: "numeric", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    calibration = table.Column<decimal>(type: "numeric", nullable: false),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closing = table.Column<decimal>(type: "numeric", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    detail_group = table.Column<int>(type: "integer", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    inv_date = table.Column<DateOnly>(type: "date", nullable: false),
                    in_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false),
                    item_code = table.Column<string>(type: "varchar(16)", nullable: false),
                    liters = table.Column<decimal>(type: "numeric", nullable: false),
                    opening = table.Column<decimal>(type: "numeric", nullable: false),
                    out_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    particulars = table.Column<string>(type: "varchar(32)", nullable: false),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    shift = table.Column<int>(type: "integer", nullable: false),
                    start = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    trans_count = table.Column<int>(type: "integer", nullable: false),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    volume = table.Column<decimal>(type: "numeric", nullable: false),
                    cust = table.Column<string>(type: "varchar(20)", nullable: true),
                    nozdown = table.Column<string>(type: "varchar(20)", nullable: false),
                    plateno = table.Column<string>(type: "varchar(20)", nullable: true),
                    pono = table.Column<string>(type: "varchar(20)", nullable: true),
                    x_corpcode = table.Column<int>(type: "integer", nullable: false),
                    x_day = table.Column<int>(type: "integer", nullable: false),
                    x_month = table.Column<int>(type: "integer", nullable: false),
                    x_nozzle = table.Column<int>(type: "integer", nullable: false),
                    x_oid = table.Column<string>(type: "varchar(20)", nullable: false),
                    x_oname = table.Column<string>(type: "varchar(20)", nullable: false),
                    x_pump = table.Column<int>(type: "integer", nullable: false),
                    x_sitecode = table.Column<int>(type: "integer", nullable: false),
                    x_tank = table.Column<int>(type: "integer", nullable: false),
                    x_transaction = table.Column<int>(type: "integer", nullable: false),
                    x_year = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fuels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lube_deliveries",
                columns: table => new
                {
                    lube_delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    cashiercode = table.Column<string>(type: "text", nullable: false),
                    createdby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createddate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    deliverydate = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "varchar(200)", nullable: false),
                    drno = table.Column<string>(type: "varchar(50)", nullable: false),
                    dtllink = table.Column<string>(type: "varchar(50)", nullable: false),
                    invoiceno = table.Column<string>(type: "varchar(50)", nullable: false),
                    piece = table.Column<int>(type: "integer", nullable: false),
                    pono = table.Column<string>(type: "varchar(50)", nullable: false),
                    productcode = table.Column<string>(type: "varchar(10)", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    rcvdby = table.Column<string>(type: "varchar(50)", nullable: false),
                    shiftnumber = table.Column<int>(type: "integer", nullable: false),
                    shiftrecid = table.Column<string>(type: "varchar(20)", nullable: false),
                    stncode = table.Column<string>(type: "varchar(5)", nullable: false),
                    suppliercode = table.Column<string>(type: "varchar(10)", nullable: false),
                    unit = table.Column<string>(type: "varchar(10)", nullable: false),
                    unitprice = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lube_deliveries", x => x.lube_delivery_id);
                });

            migrationBuilder.CreateTable(
                name: "lube_purchase_headers",
                columns: table => new
                {
                    lube_purchase_header_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cashier_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    delivery_date = table.Column<DateOnly>(type: "date", nullable: false),
                    detail_link = table.Column<string>(type: "varchar(50)", nullable: false),
                    dr_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lube_purchase_header_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    po_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    received_by = table.Column<string>(type: "varchar(50)", nullable: false),
                    sales_invoice = table.Column<string>(type: "varchar(50)", nullable: false),
                    shift_no = table.Column<int>(type: "integer", nullable: false),
                    shift_rec_id = table.Column<string>(type: "varchar(20)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    supplier_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    vatable_sales = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lube_purchase_headers", x => x.lube_purchase_header_id);
                });

            migrationBuilder.CreateTable(
                name: "lubes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    amount_db = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cashier = table.Column<string>(type: "varchar(20)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    inv_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false),
                    item_code = table.Column<string>(type: "varchar(16)", nullable: false),
                    lubes_qty = table.Column<decimal>(type: "numeric", nullable: false),
                    particulars = table.Column<string>(type: "varchar(100)", nullable: false),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    shift = table.Column<int>(type: "integer", nullable: false),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cust = table.Column<string>(type: "varchar(20)", nullable: true),
                    plateno = table.Column<string>(type: "varchar(20)", nullable: true),
                    pono = table.Column<string>(type: "varchar(20)", nullable: true),
                    x_corpcode = table.Column<int>(type: "integer", nullable: false),
                    x_day = table.Column<int>(type: "integer", nullable: false),
                    x_month = table.Column<int>(type: "integer", nullable: false),
                    x_oid = table.Column<string>(type: "varchar(10)", nullable: false),
                    x_sitecode = table.Column<int>(type: "integer", nullable: false),
                    x_stamp = table.Column<string>(type: "varchar(50)", nullable: false),
                    x_transaction = table.Column<long>(type: "bigint", nullable: false),
                    x_year = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lubes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "offlines",
                columns: table => new
                {
                    offline_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    closing = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    closing_dsr_no = table.Column<string>(type: "text", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_resolve = table.Column<bool>(type: "boolean", nullable: false),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    liters = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    new_closing = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    opening = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    opening_dsr_no = table.Column<string>(type: "text", nullable: false),
                    product = table.Column<string>(type: "varchar(20)", nullable: false),
                    pump = table.Column<int>(type: "integer", nullable: false),
                    series_no = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offlines", x => x.offline_id);
                });

            migrationBuilder.CreateTable(
                name: "po_sales",
                columns: table => new
                {
                    po_sales_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cashier_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    contract_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    customer_code = table.Column<string>(type: "varchar(20)", nullable: false),
                    dr_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    driver = table.Column<string>(type: "varchar(50)", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    po_sales_date = table.Column<DateOnly>(type: "date", nullable: false),
                    po_sales_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    po_sales_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    plate_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    product_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    shift_no = table.Column<int>(type: "integer", nullable: false),
                    shift_rec_id = table.Column<string>(type: "varchar(20)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    trip_ticket = table.Column<string>(type: "varchar(20)", nullable: false),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_po_sales", x => x.po_sales_id);
                });

            migrationBuilder.CreateTable(
                name: "po_sales_raw",
                columns: table => new
                {
                    po_sales_raw_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cashiercode = table.Column<string>(type: "text", nullable: false),
                    contractprice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    createdby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createddate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    customercode = table.Column<string>(type: "varchar(20)", nullable: false),
                    driver = table.Column<string>(type: "varchar(50)", nullable: false),
                    drnumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    plateno = table.Column<string>(type: "varchar(50)", nullable: false),
                    podate = table.Column<DateOnly>(type: "date", nullable: false),
                    potime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    productcode = table.Column<string>(type: "varchar(10)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    shiftnumber = table.Column<int>(type: "integer", nullable: false),
                    shiftrecid = table.Column<string>(type: "varchar(20)", nullable: false),
                    stncode = table.Column<string>(type: "varchar(5)", nullable: false),
                    tripticket = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_po_sales_raw", x => x.po_sales_raw_id);
                });

            migrationBuilder.CreateTable(
                name: "safe_drops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    b_date = table.Column<DateOnly>(type: "date", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    inv_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    shift = table.Column<int>(type: "integer", nullable: false),
                    t_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    x_corpcode = table.Column<int>(type: "integer", nullable: false),
                    x_day = table.Column<int>(type: "integer", nullable: false),
                    x_month = table.Column<int>(type: "integer", nullable: false),
                    x_oid = table.Column<string>(type: "varchar(10)", nullable: false),
                    x_oname = table.Column<string>(type: "varchar(20)", nullable: false),
                    x_sitecode = table.Column<int>(type: "integer", nullable: false),
                    x_stamp = table.Column<string>(type: "varchar(50)", nullable: false),
                    x_year = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_safe_drops", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sales_headers",
                columns: table => new
                {
                    sales_header_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    actual_cash_on_hand = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cashier = table.Column<string>(type: "varchar(20)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    customers = table.Column<string[]>(type: "varchar[]", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fuel_sales_total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    gain_or_loss = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_modified = table.Column<bool>(type: "boolean", nullable: false),
                    is_transaction_normal = table.Column<bool>(type: "boolean", nullable: false),
                    lubes_total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    po_sales_amount = table.Column<decimal[]>(type: "numeric(18,2)[]", nullable: false),
                    po_sales_total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    particular = table.Column<string>(type: "varchar(200)", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    safe_drop_total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    sales_no = table.Column<string>(type: "varchar(15)", nullable: false),
                    shift = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "varchar(10)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false),
                    time_in = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_out = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    total_sales = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_headers", x => x.sales_header_id);
                });

            migrationBuilder.CreateTable(
                name: "lube_purchase_details",
                columns: table => new
                {
                    lube_purchase_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lube_purchase_header_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    cost_per_case = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    cost_per_piece = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    description = table.Column<string>(type: "varchar(200)", nullable: false),
                    lube_purchase_header_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    piece = table.Column<int>(type: "integer", nullable: false),
                    product_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false),
                    unit = table.Column<string>(type: "varchar(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lube_purchase_details", x => x.lube_purchase_detail_id);
                    table.ForeignKey(
                        name: "fk_lube_purchase_details_lube_purchase_headers_lube_purchase_h",
                        column: x => x.lube_purchase_header_id,
                        principalTable: "lube_purchase_headers",
                        principalColumn: "lube_purchase_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_details",
                columns: table => new
                {
                    sales_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sales_header_id = table.Column<int>(type: "integer", nullable: false),
                    calibration = table.Column<decimal>(type: "numeric", nullable: false),
                    closing = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    liters = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    liters_sold = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    opening = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    particular = table.Column<string>(type: "varchar(50)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    product = table.Column<string>(type: "varchar(20)", nullable: false),
                    reference_no = table.Column<string>(type: "varchar(15)", nullable: true),
                    sale = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    sales_no = table.Column<string>(type: "varchar(15)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false),
                    transaction_count = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_details", x => x.sales_detail_id);
                    table.ForeignKey(
                        name: "fk_sales_details_sales_headers_sales_header_id",
                        column: x => x.sales_header_id,
                        principalTable: "sales_headers",
                        principalColumn: "sales_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fuel_deliveries_shiftrecid",
                table: "fuel_deliveries",
                column: "shiftrecid");

            migrationBuilder.CreateIndex(
                name: "ix_fuel_deliveries_stncode",
                table: "fuel_deliveries",
                column: "stncode");

            migrationBuilder.CreateIndex(
                name: "ix_fuel_purchase_fuel_purchase_no",
                table: "fuel_purchase",
                column: "fuel_purchase_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fuel_purchase_product_code",
                table: "fuel_purchase",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "ix_fuel_purchase_station_code",
                table: "fuel_purchase",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_fuels_inv_date",
                table: "fuels",
                column: "inv_date");

            migrationBuilder.CreateIndex(
                name: "ix_fuels_is_processed",
                table: "fuels",
                column: "is_processed");

            migrationBuilder.CreateIndex(
                name: "ix_fuels_item_code",
                table: "fuels",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "ix_fuels_particulars",
                table: "fuels",
                column: "particulars");

            migrationBuilder.CreateIndex(
                name: "ix_fuels_shift",
                table: "fuels",
                column: "shift");

            migrationBuilder.CreateIndex(
                name: "ix_fuels_x_oname",
                table: "fuels",
                column: "x_oname");

            migrationBuilder.CreateIndex(
                name: "ix_fuels_x_pump",
                table: "fuels",
                column: "x_pump");

            migrationBuilder.CreateIndex(
                name: "ix_fuels_x_sitecode",
                table: "fuels",
                column: "x_sitecode");

            migrationBuilder.CreateIndex(
                name: "ix_lube_deliveries_dtllink",
                table: "lube_deliveries",
                column: "dtllink");

            migrationBuilder.CreateIndex(
                name: "ix_lube_deliveries_shiftrecid",
                table: "lube_deliveries",
                column: "shiftrecid");

            migrationBuilder.CreateIndex(
                name: "ix_lube_deliveries_stncode",
                table: "lube_deliveries",
                column: "stncode");

            migrationBuilder.CreateIndex(
                name: "ix_lube_purchase_details_lube_purchase_header_id",
                table: "lube_purchase_details",
                column: "lube_purchase_header_id");

            migrationBuilder.CreateIndex(
                name: "ix_lube_purchase_details_lube_purchase_header_no",
                table: "lube_purchase_details",
                column: "lube_purchase_header_no");

            migrationBuilder.CreateIndex(
                name: "ix_lube_purchase_details_product_code",
                table: "lube_purchase_details",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "ix_lube_purchase_headers_lube_purchase_header_no",
                table: "lube_purchase_headers",
                column: "lube_purchase_header_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lube_purchase_headers_station_code",
                table: "lube_purchase_headers",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_lubes_cashier",
                table: "lubes",
                column: "cashier");

            migrationBuilder.CreateIndex(
                name: "ix_lubes_inv_date",
                table: "lubes",
                column: "inv_date");

            migrationBuilder.CreateIndex(
                name: "ix_po_sales_po_sales_no",
                table: "po_sales",
                column: "po_sales_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_po_sales_raw_shiftrecid",
                table: "po_sales_raw",
                column: "shiftrecid");

            migrationBuilder.CreateIndex(
                name: "ix_po_sales_raw_stncode",
                table: "po_sales_raw",
                column: "stncode");

            migrationBuilder.CreateIndex(
                name: "ix_po_sales_raw_tripticket",
                table: "po_sales_raw",
                column: "tripticket");

            migrationBuilder.CreateIndex(
                name: "ix_safe_drops_inv_date",
                table: "safe_drops",
                column: "inv_date");

            migrationBuilder.CreateIndex(
                name: "ix_safe_drops_x_oname",
                table: "safe_drops",
                column: "x_oname");

            migrationBuilder.CreateIndex(
                name: "ix_sales_details_sales_header_id",
                table: "sales_details",
                column: "sales_header_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_details_sales_no",
                table: "sales_details",
                column: "sales_no");

            migrationBuilder.CreateIndex(
                name: "ix_sales_details_station_code",
                table: "sales_details",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_sales_headers_cashier",
                table: "sales_headers",
                column: "cashier");

            migrationBuilder.CreateIndex(
                name: "ix_sales_headers_date",
                table: "sales_headers",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_sales_headers_sales_no",
                table: "sales_headers",
                column: "sales_no");

            migrationBuilder.CreateIndex(
                name: "ix_sales_headers_shift",
                table: "sales_headers",
                column: "shift");

            migrationBuilder.CreateIndex(
                name: "ix_sales_headers_station_code",
                table: "sales_headers",
                column: "station_code");
        }
    }
}
