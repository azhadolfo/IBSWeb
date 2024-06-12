using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ApplyTheNamingConventionOfPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_LubePurchaseDetails_LubePurchaseHeaders_LubePurchaseHeaderId",
                table: "LubePurchaseDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesDetails_SalesHeaders_SalesHeaderId",
                table: "SalesDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Stations",
                table: "Stations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Products",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Offlines",
                table: "Offlines");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Lubes",
                table: "Lubes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Inventories",
                table: "Inventories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Fuels",
                table: "Fuels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Customers",
                table: "Customers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Companies",
                table: "Companies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesHeaders",
                table: "SalesHeaders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesDetails",
                table: "SalesDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SafeDrops",
                table: "SafeDrops");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PoSalesRaw",
                table: "PoSalesRaw");

            migrationBuilder.DropPrimaryKey(
                name: "PK_POSales",
                table: "POSales");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LubePurchaseHeaders",
                table: "LubePurchaseHeaders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LubePurchaseDetails",
                table: "LubePurchaseDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LubeDeliveries",
                table: "LubeDeliveries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LogMessages",
                table: "LogMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GeneralLedgers",
                table: "GeneralLedgers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FuelPurchase",
                table: "FuelPurchase");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FuelDeliveries",
                table: "FuelDeliveries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CsvFiles",
                table: "CsvFiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChartOfAccounts",
                table: "ChartOfAccounts");

            migrationBuilder.RenameTable(
                name: "Suppliers",
                newName: "suppliers");

            migrationBuilder.RenameTable(
                name: "Stations",
                newName: "stations");

            migrationBuilder.RenameTable(
                name: "Products",
                newName: "products");

            migrationBuilder.RenameTable(
                name: "Offlines",
                newName: "offlines");

            migrationBuilder.RenameTable(
                name: "Lubes",
                newName: "lubes");

            migrationBuilder.RenameTable(
                name: "Inventories",
                newName: "inventories");

            migrationBuilder.RenameTable(
                name: "Fuels",
                newName: "fuels");

            migrationBuilder.RenameTable(
                name: "Customers",
                newName: "customers");

            migrationBuilder.RenameTable(
                name: "Companies",
                newName: "companies");

            migrationBuilder.RenameTable(
                name: "SalesHeaders",
                newName: "sales_headers");

            migrationBuilder.RenameTable(
                name: "SalesDetails",
                newName: "sales_details");

            migrationBuilder.RenameTable(
                name: "SafeDrops",
                newName: "safe_drops");

            migrationBuilder.RenameTable(
                name: "PoSalesRaw",
                newName: "po_sales_raw");

            migrationBuilder.RenameTable(
                name: "POSales",
                newName: "po_sales");

            migrationBuilder.RenameTable(
                name: "LubePurchaseHeaders",
                newName: "lube_purchase_headers");

            migrationBuilder.RenameTable(
                name: "LubePurchaseDetails",
                newName: "lube_purchase_details");

            migrationBuilder.RenameTable(
                name: "LubeDeliveries",
                newName: "lube_deliveries");

            migrationBuilder.RenameTable(
                name: "LogMessages",
                newName: "log_messages");

            migrationBuilder.RenameTable(
                name: "GeneralLedgers",
                newName: "general_ledgers");

            migrationBuilder.RenameTable(
                name: "FuelPurchase",
                newName: "fuel_purchase");

            migrationBuilder.RenameTable(
                name: "FuelDeliveries",
                newName: "fuel_deliveries");

            migrationBuilder.RenameTable(
                name: "CsvFiles",
                newName: "csv_files");

            migrationBuilder.RenameTable(
                name: "ChartOfAccounts",
                newName: "chart_of_accounts");

            migrationBuilder.RenameColumn(
                name: "VatType",
                table: "suppliers",
                newName: "vat_type");

            migrationBuilder.RenameColumn(
                name: "TaxType",
                table: "suppliers",
                newName: "tax_type");

            migrationBuilder.RenameColumn(
                name: "SupplierTin",
                table: "suppliers",
                newName: "supplier_tin");

            migrationBuilder.RenameColumn(
                name: "SupplierTerms",
                table: "suppliers",
                newName: "supplier_terms");

            migrationBuilder.RenameColumn(
                name: "SupplierName",
                table: "suppliers",
                newName: "supplier_name");

            migrationBuilder.RenameColumn(
                name: "SupplierCode",
                table: "suppliers",
                newName: "supplier_code");

            migrationBuilder.RenameColumn(
                name: "SupplierAddress",
                table: "suppliers",
                newName: "supplier_address");

            migrationBuilder.RenameColumn(
                name: "ProofOfRegistrationFilePath",
                table: "suppliers",
                newName: "proof_of_registration_file_path");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "suppliers",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "EditedDate",
                table: "suppliers",
                newName: "edited_date");

            migrationBuilder.RenameColumn(
                name: "EditedBy",
                table: "suppliers",
                newName: "edited_by");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "suppliers",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "suppliers",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "SupplierId",
                table: "suppliers",
                newName: "supplier_id");

            migrationBuilder.RenameIndex(
                name: "IX_Suppliers_SupplierName",
                table: "suppliers",
                newName: "ix_suppliers_supplier_name");

            migrationBuilder.RenameIndex(
                name: "IX_Suppliers_SupplierCode",
                table: "suppliers",
                newName: "ix_suppliers_supplier_code");

            migrationBuilder.RenameColumn(
                name: "Initial",
                table: "stations",
                newName: "initial");

            migrationBuilder.RenameColumn(
                name: "StationName",
                table: "stations",
                newName: "station_name");

            migrationBuilder.RenameColumn(
                name: "StationCode",
                table: "stations",
                newName: "station_code");

            migrationBuilder.RenameColumn(
                name: "PosCode",
                table: "stations",
                newName: "pos_code");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "stations",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "FolderPath",
                table: "stations",
                newName: "folder_path");

            migrationBuilder.RenameColumn(
                name: "EditedDate",
                table: "stations",
                newName: "edited_date");

            migrationBuilder.RenameColumn(
                name: "EditedBy",
                table: "stations",
                newName: "edited_by");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "stations",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "stations",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "StationId",
                table: "stations",
                newName: "station_id");

            migrationBuilder.RenameIndex(
                name: "IX_Stations_StationName",
                table: "stations",
                newName: "ix_stations_station_name");

            migrationBuilder.RenameIndex(
                name: "IX_Stations_StationCode",
                table: "stations",
                newName: "ix_stations_station_code");

            migrationBuilder.RenameIndex(
                name: "IX_Stations_PosCode",
                table: "stations",
                newName: "ix_stations_pos_code");

            migrationBuilder.RenameColumn(
                name: "ProductUnit",
                table: "products",
                newName: "product_unit");

            migrationBuilder.RenameColumn(
                name: "ProductName",
                table: "products",
                newName: "product_name");

            migrationBuilder.RenameColumn(
                name: "ProductCode",
                table: "products",
                newName: "product_code");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "products",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "EditedDate",
                table: "products",
                newName: "edited_date");

            migrationBuilder.RenameColumn(
                name: "EditedBy",
                table: "products",
                newName: "edited_by");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "products",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "products",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "products",
                newName: "product_id");

            migrationBuilder.RenameIndex(
                name: "IX_Products_ProductName",
                table: "products",
                newName: "ix_products_product_name");

            migrationBuilder.RenameIndex(
                name: "IX_Products_ProductCode",
                table: "products",
                newName: "ix_products_product_code");

            migrationBuilder.RenameColumn(
                name: "Pump",
                table: "offlines",
                newName: "pump");

            migrationBuilder.RenameColumn(
                name: "Product",
                table: "offlines",
                newName: "product");

            migrationBuilder.RenameColumn(
                name: "Opening",
                table: "offlines",
                newName: "opening");

            migrationBuilder.RenameColumn(
                name: "Liters",
                table: "offlines",
                newName: "liters");

            migrationBuilder.RenameColumn(
                name: "Closing",
                table: "offlines",
                newName: "closing");

            migrationBuilder.RenameColumn(
                name: "Balance",
                table: "offlines",
                newName: "balance");

            migrationBuilder.RenameColumn(
                name: "StationCode",
                table: "offlines",
                newName: "station_code");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "offlines",
                newName: "start_date");

            migrationBuilder.RenameColumn(
                name: "SeriesNo",
                table: "offlines",
                newName: "series_no");

            migrationBuilder.RenameColumn(
                name: "OpeningDSRNo",
                table: "offlines",
                newName: "opening_dsr_no");

            migrationBuilder.RenameColumn(
                name: "NewClosing",
                table: "offlines",
                newName: "new_closing");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedDate",
                table: "offlines",
                newName: "last_updated_date");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedBy",
                table: "offlines",
                newName: "last_updated_by");

            migrationBuilder.RenameColumn(
                name: "IsResolve",
                table: "offlines",
                newName: "is_resolve");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "offlines",
                newName: "end_date");

            migrationBuilder.RenameColumn(
                name: "ClosingDSRNo",
                table: "offlines",
                newName: "closing_dsr_no");

            migrationBuilder.RenameColumn(
                name: "OfflineId",
                table: "offlines",
                newName: "offline_id");

            migrationBuilder.RenameColumn(
                name: "Shift",
                table: "lubes",
                newName: "shift");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "lubes",
                newName: "price");

            migrationBuilder.RenameColumn(
                name: "Particulars",
                table: "lubes",
                newName: "particulars");

            migrationBuilder.RenameColumn(
                name: "INV_DATE",
                table: "lubes",
                newName: "inv_date");

            migrationBuilder.RenameColumn(
                name: "Cashier",
                table: "lubes",
                newName: "cashier");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "lubes",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "lubes",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "xYEAR",
                table: "lubes",
                newName: "x_year");

            migrationBuilder.RenameColumn(
                name: "xTRANSACTION",
                table: "lubes",
                newName: "x_transaction");

            migrationBuilder.RenameColumn(
                name: "xStamp",
                table: "lubes",
                newName: "x_stamp");

            migrationBuilder.RenameColumn(
                name: "xSITECODE",
                table: "lubes",
                newName: "x_sitecode");

            migrationBuilder.RenameColumn(
                name: "xOID",
                table: "lubes",
                newName: "x_oid");

            migrationBuilder.RenameColumn(
                name: "xMONTH",
                table: "lubes",
                newName: "x_month");

            migrationBuilder.RenameColumn(
                name: "xDAY",
                table: "lubes",
                newName: "x_day");

            migrationBuilder.RenameColumn(
                name: "xCORPCODE",
                table: "lubes",
                newName: "x_corpcode");

            migrationBuilder.RenameColumn(
                name: "VoidedDate",
                table: "lubes",
                newName: "voided_date");

            migrationBuilder.RenameColumn(
                name: "VoidedBy",
                table: "lubes",
                newName: "voided_by");

            migrationBuilder.RenameColumn(
                name: "PostedDate",
                table: "lubes",
                newName: "posted_date");

            migrationBuilder.RenameColumn(
                name: "PostedBy",
                table: "lubes",
                newName: "posted_by");

            migrationBuilder.RenameColumn(
                name: "LubesQty",
                table: "lubes",
                newName: "lubes_qty");

            migrationBuilder.RenameColumn(
                name: "ItemCode",
                table: "lubes",
                newName: "item_code");

            migrationBuilder.RenameColumn(
                name: "IsProcessed",
                table: "lubes",
                newName: "is_processed");

            migrationBuilder.RenameColumn(
                name: "EditedDate",
                table: "lubes",
                newName: "edited_date");

            migrationBuilder.RenameColumn(
                name: "EditedBy",
                table: "lubes",
                newName: "edited_by");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "lubes",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "lubes",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CanceledDate",
                table: "lubes",
                newName: "canceled_date");

            migrationBuilder.RenameColumn(
                name: "CanceledBy",
                table: "lubes",
                newName: "canceled_by");

            migrationBuilder.RenameColumn(
                name: "BusinessDate",
                table: "lubes",
                newName: "business_date");

            migrationBuilder.RenameColumn(
                name: "AmountDB",
                table: "lubes",
                newName: "amount_db");

            migrationBuilder.RenameIndex(
                name: "IX_Lubes_INV_DATE",
                table: "lubes",
                newName: "ix_lubes_inv_date");

            migrationBuilder.RenameIndex(
                name: "IX_Lubes_Cashier",
                table: "lubes",
                newName: "ix_lubes_cashier");

            migrationBuilder.RenameColumn(
                name: "Reference",
                table: "inventories",
                newName: "reference");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "inventories",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "Particulars",
                table: "inventories",
                newName: "particulars");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "inventories",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "ValidatedDate",
                table: "inventories",
                newName: "validated_date");

            migrationBuilder.RenameColumn(
                name: "ValidatedBy",
                table: "inventories",
                newName: "validated_by");

            migrationBuilder.RenameColumn(
                name: "UnitCostAverage",
                table: "inventories",
                newName: "unit_cost_average");

            migrationBuilder.RenameColumn(
                name: "UnitCost",
                table: "inventories",
                newName: "unit_cost");

            migrationBuilder.RenameColumn(
                name: "TransactionNo",
                table: "inventories",
                newName: "transaction_no");

            migrationBuilder.RenameColumn(
                name: "TotalCost",
                table: "inventories",
                newName: "total_cost");

            migrationBuilder.RenameColumn(
                name: "StationCode",
                table: "inventories",
                newName: "station_code");

            migrationBuilder.RenameColumn(
                name: "RunningCost",
                table: "inventories",
                newName: "running_cost");

            migrationBuilder.RenameColumn(
                name: "ProductCode",
                table: "inventories",
                newName: "product_code");

            migrationBuilder.RenameColumn(
                name: "InventoryValue",
                table: "inventories",
                newName: "inventory_value");

            migrationBuilder.RenameColumn(
                name: "InventoryBalance",
                table: "inventories",
                newName: "inventory_balance");

            migrationBuilder.RenameColumn(
                name: "CostOfGoodsSold",
                table: "inventories",
                newName: "cost_of_goods_sold");

            migrationBuilder.RenameColumn(
                name: "InventoryId",
                table: "inventories",
                newName: "inventory_id");

            migrationBuilder.RenameIndex(
                name: "IX_Inventories_TransactionNo",
                table: "inventories",
                newName: "ix_inventories_transaction_no");

            migrationBuilder.RenameIndex(
                name: "IX_Inventories_StationCode",
                table: "inventories",
                newName: "ix_inventories_station_code");

            migrationBuilder.RenameIndex(
                name: "IX_Inventories_ProductCode",
                table: "inventories",
                newName: "ix_inventories_product_code");

            migrationBuilder.RenameColumn(
                name: "Volume",
                table: "fuels",
                newName: "volume");

            migrationBuilder.RenameColumn(
                name: "Start",
                table: "fuels",
                newName: "start");

            migrationBuilder.RenameColumn(
                name: "Shift",
                table: "fuels",
                newName: "shift");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "fuels",
                newName: "price");

            migrationBuilder.RenameColumn(
                name: "Particulars",
                table: "fuels",
                newName: "particulars");

            migrationBuilder.RenameColumn(
                name: "Opening",
                table: "fuels",
                newName: "opening");

            migrationBuilder.RenameColumn(
                name: "Liters",
                table: "fuels",
                newName: "liters");

            migrationBuilder.RenameColumn(
                name: "INV_DATE",
                table: "fuels",
                newName: "inv_date");

            migrationBuilder.RenameColumn(
                name: "End",
                table: "fuels",
                newName: "end");

            migrationBuilder.RenameColumn(
                name: "Closing",
                table: "fuels",
                newName: "closing");

            migrationBuilder.RenameColumn(
                name: "Calibration",
                table: "fuels",
                newName: "calibration");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "fuels",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "fuels",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "xYEAR",
                table: "fuels",
                newName: "x_year");

            migrationBuilder.RenameColumn(
                name: "xTRANSACTION",
                table: "fuels",
                newName: "x_transaction");

            migrationBuilder.RenameColumn(
                name: "xTANK",
                table: "fuels",
                newName: "x_tank");

            migrationBuilder.RenameColumn(
                name: "xSITECODE",
                table: "fuels",
                newName: "x_sitecode");

            migrationBuilder.RenameColumn(
                name: "xPUMP",
                table: "fuels",
                newName: "x_pump");

            migrationBuilder.RenameColumn(
                name: "xONAME",
                table: "fuels",
                newName: "x_oname");

            migrationBuilder.RenameColumn(
                name: "xOID",
                table: "fuels",
                newName: "x_oid");

            migrationBuilder.RenameColumn(
                name: "xNOZZLE",
                table: "fuels",
                newName: "x_nozzle");

            migrationBuilder.RenameColumn(
                name: "xMONTH",
                table: "fuels",
                newName: "x_month");

            migrationBuilder.RenameColumn(
                name: "xDAY",
                table: "fuels",
                newName: "x_day");

            migrationBuilder.RenameColumn(
                name: "xCORPCODE",
                table: "fuels",
                newName: "x_corpcode");

            migrationBuilder.RenameColumn(
                name: "VoidedDate",
                table: "fuels",
                newName: "voided_date");

            migrationBuilder.RenameColumn(
                name: "VoidedBy",
                table: "fuels",
                newName: "voided_by");

            migrationBuilder.RenameColumn(
                name: "TransCount",
                table: "fuels",
                newName: "trans_count");

            migrationBuilder.RenameColumn(
                name: "PostedDate",
                table: "fuels",
                newName: "posted_date");

            migrationBuilder.RenameColumn(
                name: "PostedBy",
                table: "fuels",
                newName: "posted_by");

            migrationBuilder.RenameColumn(
                name: "OutTime",
                table: "fuels",
                newName: "out_time");

            migrationBuilder.RenameColumn(
                name: "ItemCode",
                table: "fuels",
                newName: "item_code");

            migrationBuilder.RenameColumn(
                name: "IsProcessed",
                table: "fuels",
                newName: "is_processed");

            migrationBuilder.RenameColumn(
                name: "InTime",
                table: "fuels",
                newName: "in_time");

            migrationBuilder.RenameColumn(
                name: "EditedDate",
                table: "fuels",
                newName: "edited_date");

            migrationBuilder.RenameColumn(
                name: "EditedBy",
                table: "fuels",
                newName: "edited_by");

            migrationBuilder.RenameColumn(
                name: "DetailGroup",
                table: "fuels",
                newName: "detail_group");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "fuels",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "fuels",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CanceledDate",
                table: "fuels",
                newName: "canceled_date");

            migrationBuilder.RenameColumn(
                name: "CanceledBy",
                table: "fuels",
                newName: "canceled_by");

            migrationBuilder.RenameColumn(
                name: "BusinessDate",
                table: "fuels",
                newName: "business_date");

            migrationBuilder.RenameColumn(
                name: "AmountDB",
                table: "fuels",
                newName: "amount_db");

            migrationBuilder.RenameIndex(
                name: "IX_Fuels_Shift",
                table: "fuels",
                newName: "ix_fuels_shift");

            migrationBuilder.RenameIndex(
                name: "IX_Fuels_Particulars",
                table: "fuels",
                newName: "ix_fuels_particulars");

            migrationBuilder.RenameIndex(
                name: "IX_Fuels_INV_DATE",
                table: "fuels",
                newName: "ix_fuels_inv_date");

            migrationBuilder.RenameIndex(
                name: "IX_Fuels_xSITECODE",
                table: "fuels",
                newName: "ix_fuels_x_sitecode");

            migrationBuilder.RenameIndex(
                name: "IX_Fuels_xPUMP",
                table: "fuels",
                newName: "ix_fuels_x_pump");

            migrationBuilder.RenameIndex(
                name: "IX_Fuels_xONAME",
                table: "fuels",
                newName: "ix_fuels_x_oname");

            migrationBuilder.RenameIndex(
                name: "IX_Fuels_ItemCode",
                table: "fuels",
                newName: "ix_fuels_item_code");

            migrationBuilder.RenameIndex(
                name: "IX_Fuels_IsProcessed",
                table: "fuels",
                newName: "ix_fuels_is_processed");

            migrationBuilder.RenameColumn(
                name: "WithHoldingVat",
                table: "customers",
                newName: "with_holding_vat");

            migrationBuilder.RenameColumn(
                name: "WithHoldingTax",
                table: "customers",
                newName: "with_holding_tax");

            migrationBuilder.RenameColumn(
                name: "VatType",
                table: "customers",
                newName: "vat_type");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "customers",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "EditedDate",
                table: "customers",
                newName: "edited_date");

            migrationBuilder.RenameColumn(
                name: "EditedBy",
                table: "customers",
                newName: "edited_by");

            migrationBuilder.RenameColumn(
                name: "CustomerType",
                table: "customers",
                newName: "customer_type");

            migrationBuilder.RenameColumn(
                name: "CustomerTin",
                table: "customers",
                newName: "customer_tin");

            migrationBuilder.RenameColumn(
                name: "CustomerTerms",
                table: "customers",
                newName: "customer_terms");

            migrationBuilder.RenameColumn(
                name: "CustomerName",
                table: "customers",
                newName: "customer_name");

            migrationBuilder.RenameColumn(
                name: "CustomerCode",
                table: "customers",
                newName: "customer_code");

            migrationBuilder.RenameColumn(
                name: "CustomerAddress",
                table: "customers",
                newName: "customer_address");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "customers",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "customers",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "BusinessStyle",
                table: "customers",
                newName: "business_style");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "customers",
                newName: "customer_id");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_CustomerName",
                table: "customers",
                newName: "ix_customers_customer_name");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_CustomerCode",
                table: "customers",
                newName: "ix_customers_customer_code");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "companies",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "EditedDate",
                table: "companies",
                newName: "edited_date");

            migrationBuilder.RenameColumn(
                name: "EditedBy",
                table: "companies",
                newName: "edited_by");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "companies",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "companies",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CompanyTin",
                table: "companies",
                newName: "company_tin");

            migrationBuilder.RenameColumn(
                name: "CompanyName",
                table: "companies",
                newName: "company_name");

            migrationBuilder.RenameColumn(
                name: "CompanyCode",
                table: "companies",
                newName: "company_code");

            migrationBuilder.RenameColumn(
                name: "CompanyAddress",
                table: "companies",
                newName: "company_address");

            migrationBuilder.RenameColumn(
                name: "BusinessStyle",
                table: "companies",
                newName: "business_style");

            migrationBuilder.RenameColumn(
                name: "CompanyId",
                table: "companies",
                newName: "company_id");

            migrationBuilder.RenameIndex(
                name: "IX_Companies_CompanyName",
                table: "companies",
                newName: "ix_companies_company_name");

            migrationBuilder.RenameIndex(
                name: "IX_Companies_CompanyCode",
                table: "companies",
                newName: "ix_companies_company_code");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "AspNetUserTokens",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "AspNetUserTokens",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                newName: "login_provider");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "AspNetUserTokens",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "AspNetUsers",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "AspNetUsers",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Discriminator",
                table: "AspNetUsers",
                newName: "discriminator");

            migrationBuilder.RenameColumn(
                name: "Department",
                table: "AspNetUsers",
                newName: "department");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AspNetUsers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "AspNetUsers",
                newName: "user_name");

            migrationBuilder.RenameColumn(
                name: "TwoFactorEnabled",
                table: "AspNetUsers",
                newName: "two_factor_enabled");

            migrationBuilder.RenameColumn(
                name: "SecurityStamp",
                table: "AspNetUsers",
                newName: "security_stamp");

            migrationBuilder.RenameColumn(
                name: "PhoneNumberConfirmed",
                table: "AspNetUsers",
                newName: "phone_number_confirmed");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "AspNetUsers",
                newName: "phone_number");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "AspNetUsers",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "NormalizedUserName",
                table: "AspNetUsers",
                newName: "normalized_user_name");

            migrationBuilder.RenameColumn(
                name: "NormalizedEmail",
                table: "AspNetUsers",
                newName: "normalized_email");

            migrationBuilder.RenameColumn(
                name: "LockoutEnd",
                table: "AspNetUsers",
                newName: "lockout_end");

            migrationBuilder.RenameColumn(
                name: "LockoutEnabled",
                table: "AspNetUsers",
                newName: "lockout_enabled");

            migrationBuilder.RenameColumn(
                name: "EmailConfirmed",
                table: "AspNetUsers",
                newName: "email_confirmed");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                table: "AspNetUsers",
                newName: "concurrency_stamp");

            migrationBuilder.RenameColumn(
                name: "AccessFailedCount",
                table: "AspNetUsers",
                newName: "access_failed_count");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "AspNetUserRoles",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "AspNetUserRoles",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                newName: "ix_asp_net_user_roles_role_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "AspNetUserLogins",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "ProviderDisplayName",
                table: "AspNetUserLogins",
                newName: "provider_display_name");

            migrationBuilder.RenameColumn(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                newName: "provider_key");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                newName: "login_provider");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                newName: "ix_asp_net_user_logins_user_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AspNetUserClaims",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "AspNetUserClaims",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "AspNetUserClaims",
                newName: "claim_value");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "AspNetUserClaims",
                newName: "claim_type");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                newName: "ix_asp_net_user_claims_user_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "AspNetRoles",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AspNetRoles",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "NormalizedName",
                table: "AspNetRoles",
                newName: "normalized_name");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                table: "AspNetRoles",
                newName: "concurrency_stamp");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AspNetRoleClaims",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "AspNetRoleClaims",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "AspNetRoleClaims",
                newName: "claim_value");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "AspNetRoleClaims",
                newName: "claim_type");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                newName: "ix_asp_net_role_claims_role_id");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "sales_headers",
                newName: "source");

            migrationBuilder.RenameColumn(
                name: "Shift",
                table: "sales_headers",
                newName: "shift");

            migrationBuilder.RenameColumn(
                name: "Particular",
                table: "sales_headers",
                newName: "particular");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "sales_headers",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Customers",
                table: "sales_headers",
                newName: "customers");

            migrationBuilder.RenameColumn(
                name: "Cashier",
                table: "sales_headers",
                newName: "cashier");

            migrationBuilder.RenameColumn(
                name: "VoidedDate",
                table: "sales_headers",
                newName: "voided_date");

            migrationBuilder.RenameColumn(
                name: "VoidedBy",
                table: "sales_headers",
                newName: "voided_by");

            migrationBuilder.RenameColumn(
                name: "TotalSales",
                table: "sales_headers",
                newName: "total_sales");

            migrationBuilder.RenameColumn(
                name: "TimeOut",
                table: "sales_headers",
                newName: "time_out");

            migrationBuilder.RenameColumn(
                name: "TimeIn",
                table: "sales_headers",
                newName: "time_in");

            migrationBuilder.RenameColumn(
                name: "StationCode",
                table: "sales_headers",
                newName: "station_code");

            migrationBuilder.RenameColumn(
                name: "SalesNo",
                table: "sales_headers",
                newName: "sales_no");

            migrationBuilder.RenameColumn(
                name: "SafeDropTotalAmount",
                table: "sales_headers",
                newName: "safe_drop_total_amount");

            migrationBuilder.RenameColumn(
                name: "PostedDate",
                table: "sales_headers",
                newName: "posted_date");

            migrationBuilder.RenameColumn(
                name: "PostedBy",
                table: "sales_headers",
                newName: "posted_by");

            migrationBuilder.RenameColumn(
                name: "POSalesTotalAmount",
                table: "sales_headers",
                newName: "po_sales_total_amount");

            migrationBuilder.RenameColumn(
                name: "POSalesAmount",
                table: "sales_headers",
                newName: "po_sales_amount");

            migrationBuilder.RenameColumn(
                name: "LubesTotalAmount",
                table: "sales_headers",
                newName: "lubes_total_amount");

            migrationBuilder.RenameColumn(
                name: "IsTransactionNormal",
                table: "sales_headers",
                newName: "is_transaction_normal");

            migrationBuilder.RenameColumn(
                name: "IsModified",
                table: "sales_headers",
                newName: "is_modified");

            migrationBuilder.RenameColumn(
                name: "GainOrLoss",
                table: "sales_headers",
                newName: "gain_or_loss");

            migrationBuilder.RenameColumn(
                name: "FuelSalesTotalAmount",
                table: "sales_headers",
                newName: "fuel_sales_total_amount");

            migrationBuilder.RenameColumn(
                name: "EditedDate",
                table: "sales_headers",
                newName: "edited_date");

            migrationBuilder.RenameColumn(
                name: "EditedBy",
                table: "sales_headers",
                newName: "edited_by");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "sales_headers",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "sales_headers",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CanceledDate",
                table: "sales_headers",
                newName: "canceled_date");

            migrationBuilder.RenameColumn(
                name: "CanceledBy",
                table: "sales_headers",
                newName: "canceled_by");

            migrationBuilder.RenameColumn(
                name: "ActualCashOnHand",
                table: "sales_headers",
                newName: "actual_cash_on_hand");

            migrationBuilder.RenameColumn(
                name: "SalesHeaderId",
                table: "sales_headers",
                newName: "sales_header_id");

            migrationBuilder.RenameIndex(
                name: "IX_SalesHeaders_StationCode",
                table: "sales_headers",
                newName: "ix_sales_headers_station_code");

            migrationBuilder.RenameIndex(
                name: "IX_SalesHeaders_Shift",
                table: "sales_headers",
                newName: "ix_sales_headers_shift");

            migrationBuilder.RenameIndex(
                name: "IX_SalesHeaders_SalesNo",
                table: "sales_headers",
                newName: "ix_sales_headers_sales_no");

            migrationBuilder.RenameIndex(
                name: "IX_SalesHeaders_Date",
                table: "sales_headers",
                newName: "ix_sales_headers_date");

            migrationBuilder.RenameIndex(
                name: "IX_SalesHeaders_Cashier",
                table: "sales_headers",
                newName: "ix_sales_headers_cashier");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "sales_details",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "Sale",
                table: "sales_details",
                newName: "sale");

            migrationBuilder.RenameColumn(
                name: "Product",
                table: "sales_details",
                newName: "product");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "sales_details",
                newName: "price");

            migrationBuilder.RenameColumn(
                name: "Particular",
                table: "sales_details",
                newName: "particular");

            migrationBuilder.RenameColumn(
                name: "Opening",
                table: "sales_details",
                newName: "opening");

            migrationBuilder.RenameColumn(
                name: "Liters",
                table: "sales_details",
                newName: "liters");

            migrationBuilder.RenameColumn(
                name: "Closing",
                table: "sales_details",
                newName: "closing");

            migrationBuilder.RenameColumn(
                name: "Calibration",
                table: "sales_details",
                newName: "calibration");

            migrationBuilder.RenameColumn(
                name: "TransactionCount",
                table: "sales_details",
                newName: "transaction_count");

            migrationBuilder.RenameColumn(
                name: "StationCode",
                table: "sales_details",
                newName: "station_code");

            migrationBuilder.RenameColumn(
                name: "SalesNo",
                table: "sales_details",
                newName: "sales_no");

            migrationBuilder.RenameColumn(
                name: "SalesHeaderId",
                table: "sales_details",
                newName: "sales_header_id");

            migrationBuilder.RenameColumn(
                name: "ReferenceNo",
                table: "sales_details",
                newName: "reference_no");

            migrationBuilder.RenameColumn(
                name: "LitersSold",
                table: "sales_details",
                newName: "liters_sold");

            migrationBuilder.RenameColumn(
                name: "SalesDetailId",
                table: "sales_details",
                newName: "sales_detail_id");

            migrationBuilder.RenameIndex(
                name: "IX_SalesDetails_StationCode",
                table: "sales_details",
                newName: "ix_sales_details_station_code");

            migrationBuilder.RenameIndex(
                name: "IX_SalesDetails_SalesNo",
                table: "sales_details",
                newName: "ix_sales_details_sales_no");

            migrationBuilder.RenameIndex(
                name: "IX_SalesDetails_SalesHeaderId",
                table: "sales_details",
                newName: "ix_sales_details_sales_header_id");

            migrationBuilder.RenameColumn(
                name: "Shift",
                table: "safe_drops",
                newName: "shift");

            migrationBuilder.RenameColumn(
                name: "INV_DATE",
                table: "safe_drops",
                newName: "inv_date");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "safe_drops",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "safe_drops",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "xYEAR",
                table: "safe_drops",
                newName: "x_year");

            migrationBuilder.RenameColumn(
                name: "xSTAMP",
                table: "safe_drops",
                newName: "x_stamp");

            migrationBuilder.RenameColumn(
                name: "xSITECODE",
                table: "safe_drops",
                newName: "x_sitecode");

            migrationBuilder.RenameColumn(
                name: "xONAME",
                table: "safe_drops",
                newName: "x_oname");

            migrationBuilder.RenameColumn(
                name: "xOID",
                table: "safe_drops",
                newName: "x_oid");

            migrationBuilder.RenameColumn(
                name: "xMONTH",
                table: "safe_drops",
                newName: "x_month");

            migrationBuilder.RenameColumn(
                name: "xDAY",
                table: "safe_drops",
                newName: "x_day");

            migrationBuilder.RenameColumn(
                name: "xCORPCODE",
                table: "safe_drops",
                newName: "x_corpcode");

            migrationBuilder.RenameColumn(
                name: "VoidedDate",
                table: "safe_drops",
                newName: "voided_date");

            migrationBuilder.RenameColumn(
                name: "VoidedBy",
                table: "safe_drops",
                newName: "voided_by");

            migrationBuilder.RenameColumn(
                name: "TTime",
                table: "safe_drops",
                newName: "t_time");

            migrationBuilder.RenameColumn(
                name: "PostedDate",
                table: "safe_drops",
                newName: "posted_date");

            migrationBuilder.RenameColumn(
                name: "PostedBy",
                table: "safe_drops",
                newName: "posted_by");

            migrationBuilder.RenameColumn(
                name: "IsProcessed",
                table: "safe_drops",
                newName: "is_processed");

            migrationBuilder.RenameColumn(
                name: "EditedDate",
                table: "safe_drops",
                newName: "edited_date");

            migrationBuilder.RenameColumn(
                name: "EditedBy",
                table: "safe_drops",
                newName: "edited_by");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "safe_drops",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "safe_drops",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CanceledDate",
                table: "safe_drops",
                newName: "canceled_date");

            migrationBuilder.RenameColumn(
                name: "CanceledBy",
                table: "safe_drops",
                newName: "canceled_by");

            migrationBuilder.RenameColumn(
                name: "BusinessDate",
                table: "safe_drops",
                newName: "business_date");

            migrationBuilder.RenameColumn(
                name: "BDate",
                table: "safe_drops",
                newName: "b_date");

            migrationBuilder.RenameIndex(
                name: "IX_SafeDrops_xONAME",
                table: "safe_drops",
                newName: "ix_safe_drops_x_oname");

            migrationBuilder.RenameIndex(
                name: "IX_SafeDrops_INV_DATE",
                table: "safe_drops",
                newName: "ix_safe_drops_inv_date");

            migrationBuilder.RenameColumn(
                name: "POSalesRawId",
                table: "po_sales_raw",
                newName: "po_sales_raw_id");

            migrationBuilder.RenameIndex(
                name: "IX_PoSalesRaw_tripticket",
                table: "po_sales_raw",
                newName: "ix_po_sales_raw_tripticket");

            migrationBuilder.RenameIndex(
                name: "IX_PoSalesRaw_stncode",
                table: "po_sales_raw",
                newName: "ix_po_sales_raw_stncode");

            migrationBuilder.RenameIndex(
                name: "IX_PoSalesRaw_shiftrecid",
                table: "po_sales_raw",
                newName: "ix_po_sales_raw_shiftrecid");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "po_sales",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "po_sales",
                newName: "price");

            migrationBuilder.RenameColumn(
                name: "Driver",
                table: "po_sales",
                newName: "driver");

            migrationBuilder.RenameColumn(
                name: "VoidedDate",
                table: "po_sales",
                newName: "voided_date");

            migrationBuilder.RenameColumn(
                name: "VoidedBy",
                table: "po_sales",
                newName: "voided_by");

            migrationBuilder.RenameColumn(
                name: "TripTicket",
                table: "po_sales",
                newName: "trip_ticket");

            migrationBuilder.RenameColumn(
                name: "StationCode",
                table: "po_sales",
                newName: "station_code");

            migrationBuilder.RenameColumn(
                name: "ShiftRecId",
                table: "po_sales",
                newName: "shift_rec_id");

            migrationBuilder.RenameColumn(
                name: "ShiftNo",
                table: "po_sales",
                newName: "shift_no");

            migrationBuilder.RenameColumn(
                name: "ProductCode",
                table: "po_sales",
                newName: "product_code");

            migrationBuilder.RenameColumn(
                name: "PostedDate",
                table: "po_sales",
                newName: "posted_date");

            migrationBuilder.RenameColumn(
                name: "PostedBy",
                table: "po_sales",
                newName: "posted_by");

            migrationBuilder.RenameColumn(
                name: "PlateNo",
                table: "po_sales",
                newName: "plate_no");

            migrationBuilder.RenameColumn(
                name: "POSalesTime",
                table: "po_sales",
                newName: "po_sales_time");

            migrationBuilder.RenameColumn(
                name: "POSalesNo",
                table: "po_sales",
                newName: "po_sales_no");

            migrationBuilder.RenameColumn(
                name: "POSalesDate",
                table: "po_sales",
                newName: "po_sales_date");

            migrationBuilder.RenameColumn(
                name: "EditedDate",
                table: "po_sales",
                newName: "edited_date");

            migrationBuilder.RenameColumn(
                name: "EditedBy",
                table: "po_sales",
                newName: "edited_by");

            migrationBuilder.RenameColumn(
                name: "DrNo",
                table: "po_sales",
                newName: "dr_no");

            migrationBuilder.RenameColumn(
                name: "CustomerCode",
                table: "po_sales",
                newName: "customer_code");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "po_sales",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "po_sales",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "ContractPrice",
                table: "po_sales",
                newName: "contract_price");

            migrationBuilder.RenameColumn(
                name: "CashierCode",
                table: "po_sales",
                newName: "cashier_code");

            migrationBuilder.RenameColumn(
                name: "CanceledDate",
                table: "po_sales",
                newName: "canceled_date");

            migrationBuilder.RenameColumn(
                name: "CanceledBy",
                table: "po_sales",
                newName: "canceled_by");

            migrationBuilder.RenameColumn(
                name: "POSalesId",
                table: "po_sales",
                newName: "po_sales_id");

            migrationBuilder.RenameIndex(
                name: "IX_POSales_POSalesNo",
                table: "po_sales",
                newName: "ix_po_sales_po_sales_no");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "lube_purchase_headers",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "VoidedDate",
                table: "lube_purchase_headers",
                newName: "voided_date");

            migrationBuilder.RenameColumn(
                name: "VoidedBy",
                table: "lube_purchase_headers",
                newName: "voided_by");

            migrationBuilder.RenameColumn(
                name: "VatableSales",
                table: "lube_purchase_headers",
                newName: "vatable_sales");

            migrationBuilder.RenameColumn(
                name: "VatAmount",
                table: "lube_purchase_headers",
                newName: "vat_amount");

            migrationBuilder.RenameColumn(
                name: "SupplierCode",
                table: "lube_purchase_headers",
                newName: "supplier_code");

            migrationBuilder.RenameColumn(
                name: "StationCode",
                table: "lube_purchase_headers",
                newName: "station_code");

            migrationBuilder.RenameColumn(
                name: "ShiftRecId",
                table: "lube_purchase_headers",
                newName: "shift_rec_id");

            migrationBuilder.RenameColumn(
                name: "ShiftNo",
                table: "lube_purchase_headers",
                newName: "shift_no");

            migrationBuilder.RenameColumn(
                name: "SalesInvoice",
                table: "lube_purchase_headers",
                newName: "sales_invoice");

            migrationBuilder.RenameColumn(
                name: "ReceivedBy",
                table: "lube_purchase_headers",
                newName: "received_by");

            migrationBuilder.RenameColumn(
                name: "PostedDate",
                table: "lube_purchase_headers",
                newName: "posted_date");

            migrationBuilder.RenameColumn(
                name: "PostedBy",
                table: "lube_purchase_headers",
                newName: "posted_by");

            migrationBuilder.RenameColumn(
                name: "PoNo",
                table: "lube_purchase_headers",
                newName: "po_no");

            migrationBuilder.RenameColumn(
                name: "LubePurchaseHeaderNo",
                table: "lube_purchase_headers",
                newName: "lube_purchase_header_no");

            migrationBuilder.RenameColumn(
                name: "EditedDate",
                table: "lube_purchase_headers",
                newName: "edited_date");

            migrationBuilder.RenameColumn(
                name: "EditedBy",
                table: "lube_purchase_headers",
                newName: "edited_by");

            migrationBuilder.RenameColumn(
                name: "DrNo",
                table: "lube_purchase_headers",
                newName: "dr_no");

            migrationBuilder.RenameColumn(
                name: "DetailLink",
                table: "lube_purchase_headers",
                newName: "detail_link");

            migrationBuilder.RenameColumn(
                name: "DeliveryDate",
                table: "lube_purchase_headers",
                newName: "delivery_date");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "lube_purchase_headers",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "lube_purchase_headers",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CashierCode",
                table: "lube_purchase_headers",
                newName: "cashier_code");

            migrationBuilder.RenameColumn(
                name: "CanceledDate",
                table: "lube_purchase_headers",
                newName: "canceled_date");

            migrationBuilder.RenameColumn(
                name: "CanceledBy",
                table: "lube_purchase_headers",
                newName: "canceled_by");

            migrationBuilder.RenameColumn(
                name: "LubePurchaseHeaderId",
                table: "lube_purchase_headers",
                newName: "lube_purchase_header_id");

            migrationBuilder.RenameIndex(
                name: "IX_LubePurchaseHeaders_StationCode",
                table: "lube_purchase_headers",
                newName: "ix_lube_purchase_headers_station_code");

            migrationBuilder.RenameIndex(
                name: "IX_LubePurchaseHeaders_LubePurchaseHeaderNo",
                table: "lube_purchase_headers",
                newName: "ix_lube_purchase_headers_lube_purchase_header_no");

            migrationBuilder.RenameColumn(
                name: "Unit",
                table: "lube_purchase_details",
                newName: "unit");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "lube_purchase_details",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "Piece",
                table: "lube_purchase_details",
                newName: "piece");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "lube_purchase_details",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "lube_purchase_details",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "StationCode",
                table: "lube_purchase_details",
                newName: "station_code");

            migrationBuilder.RenameColumn(
                name: "ProductCode",
                table: "lube_purchase_details",
                newName: "product_code");

            migrationBuilder.RenameColumn(
                name: "LubePurchaseHeaderNo",
                table: "lube_purchase_details",
                newName: "lube_purchase_header_no");

            migrationBuilder.RenameColumn(
                name: "LubePurchaseHeaderId",
                table: "lube_purchase_details",
                newName: "lube_purchase_header_id");

            migrationBuilder.RenameColumn(
                name: "CostPerPiece",
                table: "lube_purchase_details",
                newName: "cost_per_piece");

            migrationBuilder.RenameColumn(
                name: "CostPerCase",
                table: "lube_purchase_details",
                newName: "cost_per_case");

            migrationBuilder.RenameColumn(
                name: "LubePurchaseDetailId",
                table: "lube_purchase_details",
                newName: "lube_purchase_detail_id");

            migrationBuilder.RenameIndex(
                name: "IX_LubePurchaseDetails_ProductCode",
                table: "lube_purchase_details",
                newName: "ix_lube_purchase_details_product_code");

            migrationBuilder.RenameIndex(
                name: "IX_LubePurchaseDetails_LubePurchaseHeaderNo",
                table: "lube_purchase_details",
                newName: "ix_lube_purchase_details_lube_purchase_header_no");

            migrationBuilder.RenameIndex(
                name: "IX_LubePurchaseDetails_LubePurchaseHeaderId",
                table: "lube_purchase_details",
                newName: "ix_lube_purchase_details_lube_purchase_header_id");

            migrationBuilder.RenameColumn(
                name: "LubeDeliveryId",
                table: "lube_deliveries",
                newName: "lube_delivery_id");

            migrationBuilder.RenameIndex(
                name: "IX_LubeDeliveries_stncode",
                table: "lube_deliveries",
                newName: "ix_lube_deliveries_stncode");

            migrationBuilder.RenameIndex(
                name: "IX_LubeDeliveries_shiftrecid",
                table: "lube_deliveries",
                newName: "ix_lube_deliveries_shiftrecid");

            migrationBuilder.RenameIndex(
                name: "IX_LubeDeliveries_dtllink",
                table: "lube_deliveries",
                newName: "ix_lube_deliveries_dtllink");

            migrationBuilder.RenameColumn(
                name: "Message",
                table: "log_messages",
                newName: "message");

            migrationBuilder.RenameColumn(
                name: "TimeStamp",
                table: "log_messages",
                newName: "time_stamp");

            migrationBuilder.RenameColumn(
                name: "LoggerName",
                table: "log_messages",
                newName: "logger_name");

            migrationBuilder.RenameColumn(
                name: "LogLevel",
                table: "log_messages",
                newName: "log_level");

            migrationBuilder.RenameColumn(
                name: "LogId",
                table: "log_messages",
                newName: "log_id");

            migrationBuilder.RenameColumn(
                name: "Reference",
                table: "general_ledgers",
                newName: "reference");

            migrationBuilder.RenameColumn(
                name: "Particular",
                table: "general_ledgers",
                newName: "particular");

            migrationBuilder.RenameColumn(
                name: "Debit",
                table: "general_ledgers",
                newName: "debit");

            migrationBuilder.RenameColumn(
                name: "Credit",
                table: "general_ledgers",
                newName: "credit");

            migrationBuilder.RenameColumn(
                name: "TransactionDate",
                table: "general_ledgers",
                newName: "transaction_date");

            migrationBuilder.RenameColumn(
                name: "SupplierCode",
                table: "general_ledgers",
                newName: "supplier_code");

            migrationBuilder.RenameColumn(
                name: "StationCode",
                table: "general_ledgers",
                newName: "station_code");

            migrationBuilder.RenameColumn(
                name: "ProductCode",
                table: "general_ledgers",
                newName: "product_code");

            migrationBuilder.RenameColumn(
                name: "JournalReference",
                table: "general_ledgers",
                newName: "journal_reference");

            migrationBuilder.RenameColumn(
                name: "IsValidated",
                table: "general_ledgers",
                newName: "is_validated");

            migrationBuilder.RenameColumn(
                name: "CustomerCode",
                table: "general_ledgers",
                newName: "customer_code");

            migrationBuilder.RenameColumn(
                name: "AccountTitle",
                table: "general_ledgers",
                newName: "account_title");

            migrationBuilder.RenameColumn(
                name: "AccountNumber",
                table: "general_ledgers",
                newName: "account_number");

            migrationBuilder.RenameColumn(
                name: "GeneralLedgerId",
                table: "general_ledgers",
                newName: "general_ledger_id");

            migrationBuilder.RenameIndex(
                name: "IX_GeneralLedgers_TransactionDate",
                table: "general_ledgers",
                newName: "ix_general_ledgers_transaction_date");

            migrationBuilder.RenameIndex(
                name: "IX_GeneralLedgers_SupplierCode",
                table: "general_ledgers",
                newName: "ix_general_ledgers_supplier_code");

            migrationBuilder.RenameIndex(
                name: "IX_GeneralLedgers_StationCode",
                table: "general_ledgers",
                newName: "ix_general_ledgers_station_code");

            migrationBuilder.RenameIndex(
                name: "IX_GeneralLedgers_Reference",
                table: "general_ledgers",
                newName: "ix_general_ledgers_reference");

            migrationBuilder.RenameIndex(
                name: "IX_GeneralLedgers_ProductCode",
                table: "general_ledgers",
                newName: "ix_general_ledgers_product_code");

            migrationBuilder.RenameIndex(
                name: "IX_GeneralLedgers_JournalReference",
                table: "general_ledgers",
                newName: "ix_general_ledgers_journal_reference");

            migrationBuilder.RenameIndex(
                name: "IX_GeneralLedgers_CustomerCode",
                table: "general_ledgers",
                newName: "ix_general_ledgers_customer_code");

            migrationBuilder.RenameIndex(
                name: "IX_GeneralLedgers_AccountTitle",
                table: "general_ledgers",
                newName: "ix_general_ledgers_account_title");

            migrationBuilder.RenameIndex(
                name: "IX_GeneralLedgers_AccountNumber",
                table: "general_ledgers",
                newName: "ix_general_ledgers_account_number");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "fuel_purchase",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "Hauler",
                table: "fuel_purchase",
                newName: "hauler");

            migrationBuilder.RenameColumn(
                name: "Driver",
                table: "fuel_purchase",
                newName: "driver");

            migrationBuilder.RenameColumn(
                name: "WcNo",
                table: "fuel_purchase",
                newName: "wc_no");

            migrationBuilder.RenameColumn(
                name: "VoidedDate",
                table: "fuel_purchase",
                newName: "voided_date");

            migrationBuilder.RenameColumn(
                name: "VoidedBy",
                table: "fuel_purchase",
                newName: "voided_by");

            migrationBuilder.RenameColumn(
                name: "TimeOut",
                table: "fuel_purchase",
                newName: "time_out");

            migrationBuilder.RenameColumn(
                name: "TimeIn",
                table: "fuel_purchase",
                newName: "time_in");

            migrationBuilder.RenameColumn(
                name: "TankNo",
                table: "fuel_purchase",
                newName: "tank_no");

            migrationBuilder.RenameColumn(
                name: "StationCode",
                table: "fuel_purchase",
                newName: "station_code");

            migrationBuilder.RenameColumn(
                name: "ShouldBe",
                table: "fuel_purchase",
                newName: "should_be");

            migrationBuilder.RenameColumn(
                name: "ShiftRecId",
                table: "fuel_purchase",
                newName: "shift_rec_id");

            migrationBuilder.RenameColumn(
                name: "ShiftNo",
                table: "fuel_purchase",
                newName: "shift_no");

            migrationBuilder.RenameColumn(
                name: "SellingPrice",
                table: "fuel_purchase",
                newName: "selling_price");

            migrationBuilder.RenameColumn(
                name: "ReceivedBy",
                table: "fuel_purchase",
                newName: "received_by");

            migrationBuilder.RenameColumn(
                name: "QuantityBefore",
                table: "fuel_purchase",
                newName: "quantity_before");

            migrationBuilder.RenameColumn(
                name: "QuantityAfter",
                table: "fuel_purchase",
                newName: "quantity_after");

            migrationBuilder.RenameColumn(
                name: "PurchasePrice",
                table: "fuel_purchase",
                newName: "purchase_price");

            migrationBuilder.RenameColumn(
                name: "ProductCode",
                table: "fuel_purchase",
                newName: "product_code");

            migrationBuilder.RenameColumn(
                name: "PostedDate",
                table: "fuel_purchase",
                newName: "posted_date");

            migrationBuilder.RenameColumn(
                name: "PostedBy",
                table: "fuel_purchase",
                newName: "posted_by");

            migrationBuilder.RenameColumn(
                name: "PlateNo",
                table: "fuel_purchase",
                newName: "plate_no");

            migrationBuilder.RenameColumn(
                name: "GainOrLoss",
                table: "fuel_purchase",
                newName: "gain_or_loss");

            migrationBuilder.RenameColumn(
                name: "FuelPurchaseNo",
                table: "fuel_purchase",
                newName: "fuel_purchase_no");

            migrationBuilder.RenameColumn(
                name: "EditedDate",
                table: "fuel_purchase",
                newName: "edited_date");

            migrationBuilder.RenameColumn(
                name: "EditedBy",
                table: "fuel_purchase",
                newName: "edited_by");

            migrationBuilder.RenameColumn(
                name: "DrNo",
                table: "fuel_purchase",
                newName: "dr_no");

            migrationBuilder.RenameColumn(
                name: "DeliveryDate",
                table: "fuel_purchase",
                newName: "delivery_date");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "fuel_purchase",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "fuel_purchase",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CashierCode",
                table: "fuel_purchase",
                newName: "cashier_code");

            migrationBuilder.RenameColumn(
                name: "CanceledDate",
                table: "fuel_purchase",
                newName: "canceled_date");

            migrationBuilder.RenameColumn(
                name: "CanceledBy",
                table: "fuel_purchase",
                newName: "canceled_by");

            migrationBuilder.RenameColumn(
                name: "FuelPurchaseId",
                table: "fuel_purchase",
                newName: "fuel_purchase_id");

            migrationBuilder.RenameIndex(
                name: "IX_FuelPurchase_StationCode",
                table: "fuel_purchase",
                newName: "ix_fuel_purchase_station_code");

            migrationBuilder.RenameIndex(
                name: "IX_FuelPurchase_ProductCode",
                table: "fuel_purchase",
                newName: "ix_fuel_purchase_product_code");

            migrationBuilder.RenameIndex(
                name: "IX_FuelPurchase_FuelPurchaseNo",
                table: "fuel_purchase",
                newName: "ix_fuel_purchase_fuel_purchase_no");

            migrationBuilder.RenameColumn(
                name: "FuelDeliveryId",
                table: "fuel_deliveries",
                newName: "fuel_delivery_id");

            migrationBuilder.RenameIndex(
                name: "IX_FuelDeliveries_stncode",
                table: "fuel_deliveries",
                newName: "ix_fuel_deliveries_stncode");

            migrationBuilder.RenameIndex(
                name: "IX_FuelDeliveries_shiftrecid",
                table: "fuel_deliveries",
                newName: "ix_fuel_deliveries_shiftrecid");

            migrationBuilder.RenameColumn(
                name: "StationCode",
                table: "csv_files",
                newName: "station_code");

            migrationBuilder.RenameColumn(
                name: "IsUploaded",
                table: "csv_files",
                newName: "is_uploaded");

            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "csv_files",
                newName: "file_name");

            migrationBuilder.RenameColumn(
                name: "FileId",
                table: "csv_files",
                newName: "file_id");

            migrationBuilder.RenameColumn(
                name: "Parent",
                table: "chart_of_accounts",
                newName: "parent");

            migrationBuilder.RenameColumn(
                name: "Level",
                table: "chart_of_accounts",
                newName: "level");

            migrationBuilder.RenameColumn(
                name: "NormalBalance",
                table: "chart_of_accounts",
                newName: "normal_balance");

            migrationBuilder.RenameColumn(
                name: "IsMain",
                table: "chart_of_accounts",
                newName: "is_main");

            migrationBuilder.RenameColumn(
                name: "EditedDate",
                table: "chart_of_accounts",
                newName: "edited_date");

            migrationBuilder.RenameColumn(
                name: "EditedBy",
                table: "chart_of_accounts",
                newName: "edited_by");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "chart_of_accounts",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "chart_of_accounts",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "AccountType",
                table: "chart_of_accounts",
                newName: "account_type");

            migrationBuilder.RenameColumn(
                name: "AccountNumber",
                table: "chart_of_accounts",
                newName: "account_number");

            migrationBuilder.RenameColumn(
                name: "AccountName",
                table: "chart_of_accounts",
                newName: "account_name");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "chart_of_accounts",
                newName: "account_id");

            migrationBuilder.RenameIndex(
                name: "IX_ChartOfAccounts_AccountNumber",
                table: "chart_of_accounts",
                newName: "ix_chart_of_accounts_account_number");

            migrationBuilder.RenameIndex(
                name: "IX_ChartOfAccounts_AccountName",
                table: "chart_of_accounts",
                newName: "ix_chart_of_accounts_account_name");

            migrationBuilder.AddPrimaryKey(
                name: "pk_suppliers",
                table: "suppliers",
                column: "supplier_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_stations",
                table: "stations",
                column: "station_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_products",
                table: "products",
                column: "product_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_offlines",
                table: "offlines",
                column: "offline_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_lubes",
                table: "lubes",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_inventories",
                table: "inventories",
                column: "inventory_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_fuels",
                table: "fuels",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_customers",
                table: "customers",
                column: "customer_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_companies",
                table: "companies",
                column: "company_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_tokens",
                table: "AspNetUserTokens",
                columns: new[] { "user_id", "login_provider", "name" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_users",
                table: "AspNetUsers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_roles",
                table: "AspNetUserRoles",
                columns: new[] { "user_id", "role_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_logins",
                table: "AspNetUserLogins",
                columns: new[] { "login_provider", "provider_key" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_claims",
                table: "AspNetUserClaims",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_roles",
                table: "AspNetRoles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_role_claims",
                table: "AspNetRoleClaims",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sales_headers",
                table: "sales_headers",
                column: "sales_header_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sales_details",
                table: "sales_details",
                column: "sales_detail_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_safe_drops",
                table: "safe_drops",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_po_sales_raw",
                table: "po_sales_raw",
                column: "po_sales_raw_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_po_sales",
                table: "po_sales",
                column: "po_sales_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_lube_purchase_headers",
                table: "lube_purchase_headers",
                column: "lube_purchase_header_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_lube_purchase_details",
                table: "lube_purchase_details",
                column: "lube_purchase_detail_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_lube_deliveries",
                table: "lube_deliveries",
                column: "lube_delivery_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_log_messages",
                table: "log_messages",
                column: "log_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_general_ledgers",
                table: "general_ledgers",
                column: "general_ledger_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_fuel_purchase",
                table: "fuel_purchase",
                column: "fuel_purchase_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_fuel_deliveries",
                table: "fuel_deliveries",
                column: "fuel_delivery_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_csv_files",
                table: "csv_files",
                column: "file_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_chart_of_accounts",
                table: "chart_of_accounts",
                column: "account_id");

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                table: "AspNetRoleClaims",
                column: "role_id",
                principalTable: "AspNetRoles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_claims_asp_net_users_user_id",
                table: "AspNetUserClaims",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_logins_asp_net_users_user_id",
                table: "AspNetUserLogins",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                table: "AspNetUserRoles",
                column: "role_id",
                principalTable: "AspNetRoles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_roles_asp_net_users_user_id",
                table: "AspNetUserRoles",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                table: "AspNetUserTokens",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_lube_purchase_details_lube_purchase_headers_lube_purchase_h",
                table: "lube_purchase_details",
                column: "lube_purchase_header_id",
                principalTable: "lube_purchase_headers",
                principalColumn: "lube_purchase_header_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_details_sales_headers_sales_header_id",
                table: "sales_details",
                column: "sales_header_id",
                principalTable: "sales_headers",
                principalColumn: "sales_header_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_claims_asp_net_users_user_id",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_logins_asp_net_users_user_id",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_roles_asp_net_users_user_id",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                table: "AspNetUserTokens");

            migrationBuilder.DropForeignKey(
                name: "fk_lube_purchase_details_lube_purchase_headers_lube_purchase_h",
                table: "lube_purchase_details");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_details_sales_headers_sales_header_id",
                table: "sales_details");

            migrationBuilder.DropPrimaryKey(
                name: "pk_suppliers",
                table: "suppliers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_stations",
                table: "stations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_products",
                table: "products");

            migrationBuilder.DropPrimaryKey(
                name: "pk_offlines",
                table: "offlines");

            migrationBuilder.DropPrimaryKey(
                name: "pk_lubes",
                table: "lubes");

            migrationBuilder.DropPrimaryKey(
                name: "pk_inventories",
                table: "inventories");

            migrationBuilder.DropPrimaryKey(
                name: "pk_fuels",
                table: "fuels");

            migrationBuilder.DropPrimaryKey(
                name: "pk_customers",
                table: "customers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_companies",
                table: "companies");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_tokens",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_users",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_roles",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_logins",
                table: "AspNetUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_claims",
                table: "AspNetUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_roles",
                table: "AspNetRoles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_role_claims",
                table: "AspNetRoleClaims");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sales_headers",
                table: "sales_headers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sales_details",
                table: "sales_details");

            migrationBuilder.DropPrimaryKey(
                name: "pk_safe_drops",
                table: "safe_drops");

            migrationBuilder.DropPrimaryKey(
                name: "pk_po_sales_raw",
                table: "po_sales_raw");

            migrationBuilder.DropPrimaryKey(
                name: "pk_po_sales",
                table: "po_sales");

            migrationBuilder.DropPrimaryKey(
                name: "pk_lube_purchase_headers",
                table: "lube_purchase_headers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_lube_purchase_details",
                table: "lube_purchase_details");

            migrationBuilder.DropPrimaryKey(
                name: "pk_lube_deliveries",
                table: "lube_deliveries");

            migrationBuilder.DropPrimaryKey(
                name: "pk_log_messages",
                table: "log_messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_general_ledgers",
                table: "general_ledgers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_fuel_purchase",
                table: "fuel_purchase");

            migrationBuilder.DropPrimaryKey(
                name: "pk_fuel_deliveries",
                table: "fuel_deliveries");

            migrationBuilder.DropPrimaryKey(
                name: "pk_csv_files",
                table: "csv_files");

            migrationBuilder.DropPrimaryKey(
                name: "pk_chart_of_accounts",
                table: "chart_of_accounts");

            migrationBuilder.RenameTable(
                name: "suppliers",
                newName: "Suppliers");

            migrationBuilder.RenameTable(
                name: "stations",
                newName: "Stations");

            migrationBuilder.RenameTable(
                name: "products",
                newName: "Products");

            migrationBuilder.RenameTable(
                name: "offlines",
                newName: "Offlines");

            migrationBuilder.RenameTable(
                name: "lubes",
                newName: "Lubes");

            migrationBuilder.RenameTable(
                name: "inventories",
                newName: "Inventories");

            migrationBuilder.RenameTable(
                name: "fuels",
                newName: "Fuels");

            migrationBuilder.RenameTable(
                name: "customers",
                newName: "Customers");

            migrationBuilder.RenameTable(
                name: "companies",
                newName: "Companies");

            migrationBuilder.RenameTable(
                name: "sales_headers",
                newName: "SalesHeaders");

            migrationBuilder.RenameTable(
                name: "sales_details",
                newName: "SalesDetails");

            migrationBuilder.RenameTable(
                name: "safe_drops",
                newName: "SafeDrops");

            migrationBuilder.RenameTable(
                name: "po_sales_raw",
                newName: "PoSalesRaw");

            migrationBuilder.RenameTable(
                name: "po_sales",
                newName: "POSales");

            migrationBuilder.RenameTable(
                name: "lube_purchase_headers",
                newName: "LubePurchaseHeaders");

            migrationBuilder.RenameTable(
                name: "lube_purchase_details",
                newName: "LubePurchaseDetails");

            migrationBuilder.RenameTable(
                name: "lube_deliveries",
                newName: "LubeDeliveries");

            migrationBuilder.RenameTable(
                name: "log_messages",
                newName: "LogMessages");

            migrationBuilder.RenameTable(
                name: "general_ledgers",
                newName: "GeneralLedgers");

            migrationBuilder.RenameTable(
                name: "fuel_purchase",
                newName: "FuelPurchase");

            migrationBuilder.RenameTable(
                name: "fuel_deliveries",
                newName: "FuelDeliveries");

            migrationBuilder.RenameTable(
                name: "csv_files",
                newName: "CsvFiles");

            migrationBuilder.RenameTable(
                name: "chart_of_accounts",
                newName: "ChartOfAccounts");

            migrationBuilder.RenameColumn(
                name: "vat_type",
                table: "Suppliers",
                newName: "VatType");

            migrationBuilder.RenameColumn(
                name: "tax_type",
                table: "Suppliers",
                newName: "TaxType");

            migrationBuilder.RenameColumn(
                name: "supplier_tin",
                table: "Suppliers",
                newName: "SupplierTin");

            migrationBuilder.RenameColumn(
                name: "supplier_terms",
                table: "Suppliers",
                newName: "SupplierTerms");

            migrationBuilder.RenameColumn(
                name: "supplier_name",
                table: "Suppliers",
                newName: "SupplierName");

            migrationBuilder.RenameColumn(
                name: "supplier_code",
                table: "Suppliers",
                newName: "SupplierCode");

            migrationBuilder.RenameColumn(
                name: "supplier_address",
                table: "Suppliers",
                newName: "SupplierAddress");

            migrationBuilder.RenameColumn(
                name: "proof_of_registration_file_path",
                table: "Suppliers",
                newName: "ProofOfRegistrationFilePath");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Suppliers",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "edited_date",
                table: "Suppliers",
                newName: "EditedDate");

            migrationBuilder.RenameColumn(
                name: "edited_by",
                table: "Suppliers",
                newName: "EditedBy");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "Suppliers",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "Suppliers",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "supplier_id",
                table: "Suppliers",
                newName: "SupplierId");

            migrationBuilder.RenameIndex(
                name: "ix_suppliers_supplier_name",
                table: "Suppliers",
                newName: "IX_Suppliers_SupplierName");

            migrationBuilder.RenameIndex(
                name: "ix_suppliers_supplier_code",
                table: "Suppliers",
                newName: "IX_Suppliers_SupplierCode");

            migrationBuilder.RenameColumn(
                name: "initial",
                table: "Stations",
                newName: "Initial");

            migrationBuilder.RenameColumn(
                name: "station_name",
                table: "Stations",
                newName: "StationName");

            migrationBuilder.RenameColumn(
                name: "station_code",
                table: "Stations",
                newName: "StationCode");

            migrationBuilder.RenameColumn(
                name: "pos_code",
                table: "Stations",
                newName: "PosCode");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Stations",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "folder_path",
                table: "Stations",
                newName: "FolderPath");

            migrationBuilder.RenameColumn(
                name: "edited_date",
                table: "Stations",
                newName: "EditedDate");

            migrationBuilder.RenameColumn(
                name: "edited_by",
                table: "Stations",
                newName: "EditedBy");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "Stations",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "Stations",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "station_id",
                table: "Stations",
                newName: "StationId");

            migrationBuilder.RenameIndex(
                name: "ix_stations_station_name",
                table: "Stations",
                newName: "IX_Stations_StationName");

            migrationBuilder.RenameIndex(
                name: "ix_stations_station_code",
                table: "Stations",
                newName: "IX_Stations_StationCode");

            migrationBuilder.RenameIndex(
                name: "ix_stations_pos_code",
                table: "Stations",
                newName: "IX_Stations_PosCode");

            migrationBuilder.RenameColumn(
                name: "product_unit",
                table: "Products",
                newName: "ProductUnit");

            migrationBuilder.RenameColumn(
                name: "product_name",
                table: "Products",
                newName: "ProductName");

            migrationBuilder.RenameColumn(
                name: "product_code",
                table: "Products",
                newName: "ProductCode");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Products",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "edited_date",
                table: "Products",
                newName: "EditedDate");

            migrationBuilder.RenameColumn(
                name: "edited_by",
                table: "Products",
                newName: "EditedBy");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "Products",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "Products",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "product_id",
                table: "Products",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "ix_products_product_name",
                table: "Products",
                newName: "IX_Products_ProductName");

            migrationBuilder.RenameIndex(
                name: "ix_products_product_code",
                table: "Products",
                newName: "IX_Products_ProductCode");

            migrationBuilder.RenameColumn(
                name: "pump",
                table: "Offlines",
                newName: "Pump");

            migrationBuilder.RenameColumn(
                name: "product",
                table: "Offlines",
                newName: "Product");

            migrationBuilder.RenameColumn(
                name: "opening",
                table: "Offlines",
                newName: "Opening");

            migrationBuilder.RenameColumn(
                name: "liters",
                table: "Offlines",
                newName: "Liters");

            migrationBuilder.RenameColumn(
                name: "closing",
                table: "Offlines",
                newName: "Closing");

            migrationBuilder.RenameColumn(
                name: "balance",
                table: "Offlines",
                newName: "Balance");

            migrationBuilder.RenameColumn(
                name: "station_code",
                table: "Offlines",
                newName: "StationCode");

            migrationBuilder.RenameColumn(
                name: "start_date",
                table: "Offlines",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "series_no",
                table: "Offlines",
                newName: "SeriesNo");

            migrationBuilder.RenameColumn(
                name: "opening_dsr_no",
                table: "Offlines",
                newName: "OpeningDSRNo");

            migrationBuilder.RenameColumn(
                name: "new_closing",
                table: "Offlines",
                newName: "NewClosing");

            migrationBuilder.RenameColumn(
                name: "last_updated_date",
                table: "Offlines",
                newName: "LastUpdatedDate");

            migrationBuilder.RenameColumn(
                name: "last_updated_by",
                table: "Offlines",
                newName: "LastUpdatedBy");

            migrationBuilder.RenameColumn(
                name: "is_resolve",
                table: "Offlines",
                newName: "IsResolve");

            migrationBuilder.RenameColumn(
                name: "end_date",
                table: "Offlines",
                newName: "EndDate");

            migrationBuilder.RenameColumn(
                name: "closing_dsr_no",
                table: "Offlines",
                newName: "ClosingDSRNo");

            migrationBuilder.RenameColumn(
                name: "offline_id",
                table: "Offlines",
                newName: "OfflineId");

            migrationBuilder.RenameColumn(
                name: "shift",
                table: "Lubes",
                newName: "Shift");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "Lubes",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "particulars",
                table: "Lubes",
                newName: "Particulars");

            migrationBuilder.RenameColumn(
                name: "inv_date",
                table: "Lubes",
                newName: "INV_DATE");

            migrationBuilder.RenameColumn(
                name: "cashier",
                table: "Lubes",
                newName: "Cashier");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "Lubes",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Lubes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "x_year",
                table: "Lubes",
                newName: "xYEAR");

            migrationBuilder.RenameColumn(
                name: "x_transaction",
                table: "Lubes",
                newName: "xTRANSACTION");

            migrationBuilder.RenameColumn(
                name: "x_stamp",
                table: "Lubes",
                newName: "xStamp");

            migrationBuilder.RenameColumn(
                name: "x_sitecode",
                table: "Lubes",
                newName: "xSITECODE");

            migrationBuilder.RenameColumn(
                name: "x_oid",
                table: "Lubes",
                newName: "xOID");

            migrationBuilder.RenameColumn(
                name: "x_month",
                table: "Lubes",
                newName: "xMONTH");

            migrationBuilder.RenameColumn(
                name: "x_day",
                table: "Lubes",
                newName: "xDAY");

            migrationBuilder.RenameColumn(
                name: "x_corpcode",
                table: "Lubes",
                newName: "xCORPCODE");

            migrationBuilder.RenameColumn(
                name: "voided_date",
                table: "Lubes",
                newName: "VoidedDate");

            migrationBuilder.RenameColumn(
                name: "voided_by",
                table: "Lubes",
                newName: "VoidedBy");

            migrationBuilder.RenameColumn(
                name: "posted_date",
                table: "Lubes",
                newName: "PostedDate");

            migrationBuilder.RenameColumn(
                name: "posted_by",
                table: "Lubes",
                newName: "PostedBy");

            migrationBuilder.RenameColumn(
                name: "lubes_qty",
                table: "Lubes",
                newName: "LubesQty");

            migrationBuilder.RenameColumn(
                name: "item_code",
                table: "Lubes",
                newName: "ItemCode");

            migrationBuilder.RenameColumn(
                name: "is_processed",
                table: "Lubes",
                newName: "IsProcessed");

            migrationBuilder.RenameColumn(
                name: "edited_date",
                table: "Lubes",
                newName: "EditedDate");

            migrationBuilder.RenameColumn(
                name: "edited_by",
                table: "Lubes",
                newName: "EditedBy");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "Lubes",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "Lubes",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "canceled_date",
                table: "Lubes",
                newName: "CanceledDate");

            migrationBuilder.RenameColumn(
                name: "canceled_by",
                table: "Lubes",
                newName: "CanceledBy");

            migrationBuilder.RenameColumn(
                name: "business_date",
                table: "Lubes",
                newName: "BusinessDate");

            migrationBuilder.RenameColumn(
                name: "amount_db",
                table: "Lubes",
                newName: "AmountDB");

            migrationBuilder.RenameIndex(
                name: "ix_lubes_inv_date",
                table: "Lubes",
                newName: "IX_Lubes_INV_DATE");

            migrationBuilder.RenameIndex(
                name: "ix_lubes_cashier",
                table: "Lubes",
                newName: "IX_Lubes_Cashier");

            migrationBuilder.RenameColumn(
                name: "reference",
                table: "Inventories",
                newName: "Reference");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "Inventories",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "particulars",
                table: "Inventories",
                newName: "Particulars");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "Inventories",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "validated_date",
                table: "Inventories",
                newName: "ValidatedDate");

            migrationBuilder.RenameColumn(
                name: "validated_by",
                table: "Inventories",
                newName: "ValidatedBy");

            migrationBuilder.RenameColumn(
                name: "unit_cost_average",
                table: "Inventories",
                newName: "UnitCostAverage");

            migrationBuilder.RenameColumn(
                name: "unit_cost",
                table: "Inventories",
                newName: "UnitCost");

            migrationBuilder.RenameColumn(
                name: "transaction_no",
                table: "Inventories",
                newName: "TransactionNo");

            migrationBuilder.RenameColumn(
                name: "total_cost",
                table: "Inventories",
                newName: "TotalCost");

            migrationBuilder.RenameColumn(
                name: "station_code",
                table: "Inventories",
                newName: "StationCode");

            migrationBuilder.RenameColumn(
                name: "running_cost",
                table: "Inventories",
                newName: "RunningCost");

            migrationBuilder.RenameColumn(
                name: "product_code",
                table: "Inventories",
                newName: "ProductCode");

            migrationBuilder.RenameColumn(
                name: "inventory_value",
                table: "Inventories",
                newName: "InventoryValue");

            migrationBuilder.RenameColumn(
                name: "inventory_balance",
                table: "Inventories",
                newName: "InventoryBalance");

            migrationBuilder.RenameColumn(
                name: "cost_of_goods_sold",
                table: "Inventories",
                newName: "CostOfGoodsSold");

            migrationBuilder.RenameColumn(
                name: "inventory_id",
                table: "Inventories",
                newName: "InventoryId");

            migrationBuilder.RenameIndex(
                name: "ix_inventories_transaction_no",
                table: "Inventories",
                newName: "IX_Inventories_TransactionNo");

            migrationBuilder.RenameIndex(
                name: "ix_inventories_station_code",
                table: "Inventories",
                newName: "IX_Inventories_StationCode");

            migrationBuilder.RenameIndex(
                name: "ix_inventories_product_code",
                table: "Inventories",
                newName: "IX_Inventories_ProductCode");

            migrationBuilder.RenameColumn(
                name: "volume",
                table: "Fuels",
                newName: "Volume");

            migrationBuilder.RenameColumn(
                name: "start",
                table: "Fuels",
                newName: "Start");

            migrationBuilder.RenameColumn(
                name: "shift",
                table: "Fuels",
                newName: "Shift");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "Fuels",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "particulars",
                table: "Fuels",
                newName: "Particulars");

            migrationBuilder.RenameColumn(
                name: "opening",
                table: "Fuels",
                newName: "Opening");

            migrationBuilder.RenameColumn(
                name: "liters",
                table: "Fuels",
                newName: "Liters");

            migrationBuilder.RenameColumn(
                name: "inv_date",
                table: "Fuels",
                newName: "INV_DATE");

            migrationBuilder.RenameColumn(
                name: "end",
                table: "Fuels",
                newName: "End");

            migrationBuilder.RenameColumn(
                name: "closing",
                table: "Fuels",
                newName: "Closing");

            migrationBuilder.RenameColumn(
                name: "calibration",
                table: "Fuels",
                newName: "Calibration");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "Fuels",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Fuels",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "x_year",
                table: "Fuels",
                newName: "xYEAR");

            migrationBuilder.RenameColumn(
                name: "x_transaction",
                table: "Fuels",
                newName: "xTRANSACTION");

            migrationBuilder.RenameColumn(
                name: "x_tank",
                table: "Fuels",
                newName: "xTANK");

            migrationBuilder.RenameColumn(
                name: "x_sitecode",
                table: "Fuels",
                newName: "xSITECODE");

            migrationBuilder.RenameColumn(
                name: "x_pump",
                table: "Fuels",
                newName: "xPUMP");

            migrationBuilder.RenameColumn(
                name: "x_oname",
                table: "Fuels",
                newName: "xONAME");

            migrationBuilder.RenameColumn(
                name: "x_oid",
                table: "Fuels",
                newName: "xOID");

            migrationBuilder.RenameColumn(
                name: "x_nozzle",
                table: "Fuels",
                newName: "xNOZZLE");

            migrationBuilder.RenameColumn(
                name: "x_month",
                table: "Fuels",
                newName: "xMONTH");

            migrationBuilder.RenameColumn(
                name: "x_day",
                table: "Fuels",
                newName: "xDAY");

            migrationBuilder.RenameColumn(
                name: "x_corpcode",
                table: "Fuels",
                newName: "xCORPCODE");

            migrationBuilder.RenameColumn(
                name: "voided_date",
                table: "Fuels",
                newName: "VoidedDate");

            migrationBuilder.RenameColumn(
                name: "voided_by",
                table: "Fuels",
                newName: "VoidedBy");

            migrationBuilder.RenameColumn(
                name: "trans_count",
                table: "Fuels",
                newName: "TransCount");

            migrationBuilder.RenameColumn(
                name: "posted_date",
                table: "Fuels",
                newName: "PostedDate");

            migrationBuilder.RenameColumn(
                name: "posted_by",
                table: "Fuels",
                newName: "PostedBy");

            migrationBuilder.RenameColumn(
                name: "out_time",
                table: "Fuels",
                newName: "OutTime");

            migrationBuilder.RenameColumn(
                name: "item_code",
                table: "Fuels",
                newName: "ItemCode");

            migrationBuilder.RenameColumn(
                name: "is_processed",
                table: "Fuels",
                newName: "IsProcessed");

            migrationBuilder.RenameColumn(
                name: "in_time",
                table: "Fuels",
                newName: "InTime");

            migrationBuilder.RenameColumn(
                name: "edited_date",
                table: "Fuels",
                newName: "EditedDate");

            migrationBuilder.RenameColumn(
                name: "edited_by",
                table: "Fuels",
                newName: "EditedBy");

            migrationBuilder.RenameColumn(
                name: "detail_group",
                table: "Fuels",
                newName: "DetailGroup");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "Fuels",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "Fuels",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "canceled_date",
                table: "Fuels",
                newName: "CanceledDate");

            migrationBuilder.RenameColumn(
                name: "canceled_by",
                table: "Fuels",
                newName: "CanceledBy");

            migrationBuilder.RenameColumn(
                name: "business_date",
                table: "Fuels",
                newName: "BusinessDate");

            migrationBuilder.RenameColumn(
                name: "amount_db",
                table: "Fuels",
                newName: "AmountDB");

            migrationBuilder.RenameIndex(
                name: "ix_fuels_shift",
                table: "Fuels",
                newName: "IX_Fuels_Shift");

            migrationBuilder.RenameIndex(
                name: "ix_fuels_particulars",
                table: "Fuels",
                newName: "IX_Fuels_Particulars");

            migrationBuilder.RenameIndex(
                name: "ix_fuels_inv_date",
                table: "Fuels",
                newName: "IX_Fuels_INV_DATE");

            migrationBuilder.RenameIndex(
                name: "ix_fuels_x_sitecode",
                table: "Fuels",
                newName: "IX_Fuels_xSITECODE");

            migrationBuilder.RenameIndex(
                name: "ix_fuels_x_pump",
                table: "Fuels",
                newName: "IX_Fuels_xPUMP");

            migrationBuilder.RenameIndex(
                name: "ix_fuels_x_oname",
                table: "Fuels",
                newName: "IX_Fuels_xONAME");

            migrationBuilder.RenameIndex(
                name: "ix_fuels_item_code",
                table: "Fuels",
                newName: "IX_Fuels_ItemCode");

            migrationBuilder.RenameIndex(
                name: "ix_fuels_is_processed",
                table: "Fuels",
                newName: "IX_Fuels_IsProcessed");

            migrationBuilder.RenameColumn(
                name: "with_holding_vat",
                table: "Customers",
                newName: "WithHoldingVat");

            migrationBuilder.RenameColumn(
                name: "with_holding_tax",
                table: "Customers",
                newName: "WithHoldingTax");

            migrationBuilder.RenameColumn(
                name: "vat_type",
                table: "Customers",
                newName: "VatType");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Customers",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "edited_date",
                table: "Customers",
                newName: "EditedDate");

            migrationBuilder.RenameColumn(
                name: "edited_by",
                table: "Customers",
                newName: "EditedBy");

            migrationBuilder.RenameColumn(
                name: "customer_type",
                table: "Customers",
                newName: "CustomerType");

            migrationBuilder.RenameColumn(
                name: "customer_tin",
                table: "Customers",
                newName: "CustomerTin");

            migrationBuilder.RenameColumn(
                name: "customer_terms",
                table: "Customers",
                newName: "CustomerTerms");

            migrationBuilder.RenameColumn(
                name: "customer_name",
                table: "Customers",
                newName: "CustomerName");

            migrationBuilder.RenameColumn(
                name: "customer_code",
                table: "Customers",
                newName: "CustomerCode");

            migrationBuilder.RenameColumn(
                name: "customer_address",
                table: "Customers",
                newName: "CustomerAddress");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "Customers",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "Customers",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "business_style",
                table: "Customers",
                newName: "BusinessStyle");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                table: "Customers",
                newName: "CustomerId");

            migrationBuilder.RenameIndex(
                name: "ix_customers_customer_name",
                table: "Customers",
                newName: "IX_Customers_CustomerName");

            migrationBuilder.RenameIndex(
                name: "ix_customers_customer_code",
                table: "Customers",
                newName: "IX_Customers_CustomerCode");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Companies",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "edited_date",
                table: "Companies",
                newName: "EditedDate");

            migrationBuilder.RenameColumn(
                name: "edited_by",
                table: "Companies",
                newName: "EditedBy");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "Companies",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "Companies",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "company_tin",
                table: "Companies",
                newName: "CompanyTin");

            migrationBuilder.RenameColumn(
                name: "company_name",
                table: "Companies",
                newName: "CompanyName");

            migrationBuilder.RenameColumn(
                name: "company_code",
                table: "Companies",
                newName: "CompanyCode");

            migrationBuilder.RenameColumn(
                name: "company_address",
                table: "Companies",
                newName: "CompanyAddress");

            migrationBuilder.RenameColumn(
                name: "business_style",
                table: "Companies",
                newName: "BusinessStyle");

            migrationBuilder.RenameColumn(
                name: "company_id",
                table: "Companies",
                newName: "CompanyId");

            migrationBuilder.RenameIndex(
                name: "ix_companies_company_name",
                table: "Companies",
                newName: "IX_Companies_CompanyName");

            migrationBuilder.RenameIndex(
                name: "ix_companies_company_code",
                table: "Companies",
                newName: "IX_Companies_CompanyCode");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "AspNetUserTokens",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "AspNetUserTokens",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "login_provider",
                table: "AspNetUserTokens",
                newName: "LoginProvider");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserTokens",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "AspNetUsers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "AspNetUsers",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "discriminator",
                table: "AspNetUsers",
                newName: "Discriminator");

            migrationBuilder.RenameColumn(
                name: "department",
                table: "AspNetUsers",
                newName: "Department");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetUsers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_name",
                table: "AspNetUsers",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "two_factor_enabled",
                table: "AspNetUsers",
                newName: "TwoFactorEnabled");

            migrationBuilder.RenameColumn(
                name: "security_stamp",
                table: "AspNetUsers",
                newName: "SecurityStamp");

            migrationBuilder.RenameColumn(
                name: "phone_number_confirmed",
                table: "AspNetUsers",
                newName: "PhoneNumberConfirmed");

            migrationBuilder.RenameColumn(
                name: "phone_number",
                table: "AspNetUsers",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "AspNetUsers",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "normalized_user_name",
                table: "AspNetUsers",
                newName: "NormalizedUserName");

            migrationBuilder.RenameColumn(
                name: "normalized_email",
                table: "AspNetUsers",
                newName: "NormalizedEmail");

            migrationBuilder.RenameColumn(
                name: "lockout_end",
                table: "AspNetUsers",
                newName: "LockoutEnd");

            migrationBuilder.RenameColumn(
                name: "lockout_enabled",
                table: "AspNetUsers",
                newName: "LockoutEnabled");

            migrationBuilder.RenameColumn(
                name: "email_confirmed",
                table: "AspNetUsers",
                newName: "EmailConfirmed");

            migrationBuilder.RenameColumn(
                name: "concurrency_stamp",
                table: "AspNetUsers",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameColumn(
                name: "access_failed_count",
                table: "AspNetUsers",
                newName: "AccessFailedCount");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "AspNetUserRoles",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserRoles",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "AspNetUserRoles",
                newName: "IX_AspNetUserRoles_RoleId");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserLogins",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "provider_display_name",
                table: "AspNetUserLogins",
                newName: "ProviderDisplayName");

            migrationBuilder.RenameColumn(
                name: "provider_key",
                table: "AspNetUserLogins",
                newName: "ProviderKey");

            migrationBuilder.RenameColumn(
                name: "login_provider",
                table: "AspNetUserLogins",
                newName: "LoginProvider");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "AspNetUserLogins",
                newName: "IX_AspNetUserLogins_UserId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetUserClaims",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserClaims",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "claim_value",
                table: "AspNetUserClaims",
                newName: "ClaimValue");

            migrationBuilder.RenameColumn(
                name: "claim_type",
                table: "AspNetUserClaims",
                newName: "ClaimType");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "AspNetUserClaims",
                newName: "IX_AspNetUserClaims_UserId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "AspNetRoles",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetRoles",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "normalized_name",
                table: "AspNetRoles",
                newName: "NormalizedName");

            migrationBuilder.RenameColumn(
                name: "concurrency_stamp",
                table: "AspNetRoles",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetRoleClaims",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "AspNetRoleClaims",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "claim_value",
                table: "AspNetRoleClaims",
                newName: "ClaimValue");

            migrationBuilder.RenameColumn(
                name: "claim_type",
                table: "AspNetRoleClaims",
                newName: "ClaimType");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "AspNetRoleClaims",
                newName: "IX_AspNetRoleClaims_RoleId");

            migrationBuilder.RenameColumn(
                name: "source",
                table: "SalesHeaders",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "shift",
                table: "SalesHeaders",
                newName: "Shift");

            migrationBuilder.RenameColumn(
                name: "particular",
                table: "SalesHeaders",
                newName: "Particular");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "SalesHeaders",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "customers",
                table: "SalesHeaders",
                newName: "Customers");

            migrationBuilder.RenameColumn(
                name: "cashier",
                table: "SalesHeaders",
                newName: "Cashier");

            migrationBuilder.RenameColumn(
                name: "voided_date",
                table: "SalesHeaders",
                newName: "VoidedDate");

            migrationBuilder.RenameColumn(
                name: "voided_by",
                table: "SalesHeaders",
                newName: "VoidedBy");

            migrationBuilder.RenameColumn(
                name: "total_sales",
                table: "SalesHeaders",
                newName: "TotalSales");

            migrationBuilder.RenameColumn(
                name: "time_out",
                table: "SalesHeaders",
                newName: "TimeOut");

            migrationBuilder.RenameColumn(
                name: "time_in",
                table: "SalesHeaders",
                newName: "TimeIn");

            migrationBuilder.RenameColumn(
                name: "station_code",
                table: "SalesHeaders",
                newName: "StationCode");

            migrationBuilder.RenameColumn(
                name: "sales_no",
                table: "SalesHeaders",
                newName: "SalesNo");

            migrationBuilder.RenameColumn(
                name: "safe_drop_total_amount",
                table: "SalesHeaders",
                newName: "SafeDropTotalAmount");

            migrationBuilder.RenameColumn(
                name: "posted_date",
                table: "SalesHeaders",
                newName: "PostedDate");

            migrationBuilder.RenameColumn(
                name: "posted_by",
                table: "SalesHeaders",
                newName: "PostedBy");

            migrationBuilder.RenameColumn(
                name: "po_sales_total_amount",
                table: "SalesHeaders",
                newName: "POSalesTotalAmount");

            migrationBuilder.RenameColumn(
                name: "po_sales_amount",
                table: "SalesHeaders",
                newName: "POSalesAmount");

            migrationBuilder.RenameColumn(
                name: "lubes_total_amount",
                table: "SalesHeaders",
                newName: "LubesTotalAmount");

            migrationBuilder.RenameColumn(
                name: "is_transaction_normal",
                table: "SalesHeaders",
                newName: "IsTransactionNormal");

            migrationBuilder.RenameColumn(
                name: "is_modified",
                table: "SalesHeaders",
                newName: "IsModified");

            migrationBuilder.RenameColumn(
                name: "gain_or_loss",
                table: "SalesHeaders",
                newName: "GainOrLoss");

            migrationBuilder.RenameColumn(
                name: "fuel_sales_total_amount",
                table: "SalesHeaders",
                newName: "FuelSalesTotalAmount");

            migrationBuilder.RenameColumn(
                name: "edited_date",
                table: "SalesHeaders",
                newName: "EditedDate");

            migrationBuilder.RenameColumn(
                name: "edited_by",
                table: "SalesHeaders",
                newName: "EditedBy");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "SalesHeaders",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "SalesHeaders",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "canceled_date",
                table: "SalesHeaders",
                newName: "CanceledDate");

            migrationBuilder.RenameColumn(
                name: "canceled_by",
                table: "SalesHeaders",
                newName: "CanceledBy");

            migrationBuilder.RenameColumn(
                name: "actual_cash_on_hand",
                table: "SalesHeaders",
                newName: "ActualCashOnHand");

            migrationBuilder.RenameColumn(
                name: "sales_header_id",
                table: "SalesHeaders",
                newName: "SalesHeaderId");

            migrationBuilder.RenameIndex(
                name: "ix_sales_headers_station_code",
                table: "SalesHeaders",
                newName: "IX_SalesHeaders_StationCode");

            migrationBuilder.RenameIndex(
                name: "ix_sales_headers_shift",
                table: "SalesHeaders",
                newName: "IX_SalesHeaders_Shift");

            migrationBuilder.RenameIndex(
                name: "ix_sales_headers_sales_no",
                table: "SalesHeaders",
                newName: "IX_SalesHeaders_SalesNo");

            migrationBuilder.RenameIndex(
                name: "ix_sales_headers_date",
                table: "SalesHeaders",
                newName: "IX_SalesHeaders_Date");

            migrationBuilder.RenameIndex(
                name: "ix_sales_headers_cashier",
                table: "SalesHeaders",
                newName: "IX_SalesHeaders_Cashier");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "SalesDetails",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "sale",
                table: "SalesDetails",
                newName: "Sale");

            migrationBuilder.RenameColumn(
                name: "product",
                table: "SalesDetails",
                newName: "Product");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "SalesDetails",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "particular",
                table: "SalesDetails",
                newName: "Particular");

            migrationBuilder.RenameColumn(
                name: "opening",
                table: "SalesDetails",
                newName: "Opening");

            migrationBuilder.RenameColumn(
                name: "liters",
                table: "SalesDetails",
                newName: "Liters");

            migrationBuilder.RenameColumn(
                name: "closing",
                table: "SalesDetails",
                newName: "Closing");

            migrationBuilder.RenameColumn(
                name: "calibration",
                table: "SalesDetails",
                newName: "Calibration");

            migrationBuilder.RenameColumn(
                name: "transaction_count",
                table: "SalesDetails",
                newName: "TransactionCount");

            migrationBuilder.RenameColumn(
                name: "station_code",
                table: "SalesDetails",
                newName: "StationCode");

            migrationBuilder.RenameColumn(
                name: "sales_no",
                table: "SalesDetails",
                newName: "SalesNo");

            migrationBuilder.RenameColumn(
                name: "sales_header_id",
                table: "SalesDetails",
                newName: "SalesHeaderId");

            migrationBuilder.RenameColumn(
                name: "reference_no",
                table: "SalesDetails",
                newName: "ReferenceNo");

            migrationBuilder.RenameColumn(
                name: "liters_sold",
                table: "SalesDetails",
                newName: "LitersSold");

            migrationBuilder.RenameColumn(
                name: "sales_detail_id",
                table: "SalesDetails",
                newName: "SalesDetailId");

            migrationBuilder.RenameIndex(
                name: "ix_sales_details_station_code",
                table: "SalesDetails",
                newName: "IX_SalesDetails_StationCode");

            migrationBuilder.RenameIndex(
                name: "ix_sales_details_sales_no",
                table: "SalesDetails",
                newName: "IX_SalesDetails_SalesNo");

            migrationBuilder.RenameIndex(
                name: "ix_sales_details_sales_header_id",
                table: "SalesDetails",
                newName: "IX_SalesDetails_SalesHeaderId");

            migrationBuilder.RenameColumn(
                name: "shift",
                table: "SafeDrops",
                newName: "Shift");

            migrationBuilder.RenameColumn(
                name: "inv_date",
                table: "SafeDrops",
                newName: "INV_DATE");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "SafeDrops",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "SafeDrops",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "x_year",
                table: "SafeDrops",
                newName: "xYEAR");

            migrationBuilder.RenameColumn(
                name: "x_stamp",
                table: "SafeDrops",
                newName: "xSTAMP");

            migrationBuilder.RenameColumn(
                name: "x_sitecode",
                table: "SafeDrops",
                newName: "xSITECODE");

            migrationBuilder.RenameColumn(
                name: "x_oname",
                table: "SafeDrops",
                newName: "xONAME");

            migrationBuilder.RenameColumn(
                name: "x_oid",
                table: "SafeDrops",
                newName: "xOID");

            migrationBuilder.RenameColumn(
                name: "x_month",
                table: "SafeDrops",
                newName: "xMONTH");

            migrationBuilder.RenameColumn(
                name: "x_day",
                table: "SafeDrops",
                newName: "xDAY");

            migrationBuilder.RenameColumn(
                name: "x_corpcode",
                table: "SafeDrops",
                newName: "xCORPCODE");

            migrationBuilder.RenameColumn(
                name: "voided_date",
                table: "SafeDrops",
                newName: "VoidedDate");

            migrationBuilder.RenameColumn(
                name: "voided_by",
                table: "SafeDrops",
                newName: "VoidedBy");

            migrationBuilder.RenameColumn(
                name: "t_time",
                table: "SafeDrops",
                newName: "TTime");

            migrationBuilder.RenameColumn(
                name: "posted_date",
                table: "SafeDrops",
                newName: "PostedDate");

            migrationBuilder.RenameColumn(
                name: "posted_by",
                table: "SafeDrops",
                newName: "PostedBy");

            migrationBuilder.RenameColumn(
                name: "is_processed",
                table: "SafeDrops",
                newName: "IsProcessed");

            migrationBuilder.RenameColumn(
                name: "edited_date",
                table: "SafeDrops",
                newName: "EditedDate");

            migrationBuilder.RenameColumn(
                name: "edited_by",
                table: "SafeDrops",
                newName: "EditedBy");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "SafeDrops",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "SafeDrops",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "canceled_date",
                table: "SafeDrops",
                newName: "CanceledDate");

            migrationBuilder.RenameColumn(
                name: "canceled_by",
                table: "SafeDrops",
                newName: "CanceledBy");

            migrationBuilder.RenameColumn(
                name: "business_date",
                table: "SafeDrops",
                newName: "BusinessDate");

            migrationBuilder.RenameColumn(
                name: "b_date",
                table: "SafeDrops",
                newName: "BDate");

            migrationBuilder.RenameIndex(
                name: "ix_safe_drops_x_oname",
                table: "SafeDrops",
                newName: "IX_SafeDrops_xONAME");

            migrationBuilder.RenameIndex(
                name: "ix_safe_drops_inv_date",
                table: "SafeDrops",
                newName: "IX_SafeDrops_INV_DATE");

            migrationBuilder.RenameColumn(
                name: "po_sales_raw_id",
                table: "PoSalesRaw",
                newName: "POSalesRawId");

            migrationBuilder.RenameIndex(
                name: "ix_po_sales_raw_tripticket",
                table: "PoSalesRaw",
                newName: "IX_PoSalesRaw_tripticket");

            migrationBuilder.RenameIndex(
                name: "ix_po_sales_raw_stncode",
                table: "PoSalesRaw",
                newName: "IX_PoSalesRaw_stncode");

            migrationBuilder.RenameIndex(
                name: "ix_po_sales_raw_shiftrecid",
                table: "PoSalesRaw",
                newName: "IX_PoSalesRaw_shiftrecid");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "POSales",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "POSales",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "driver",
                table: "POSales",
                newName: "Driver");

            migrationBuilder.RenameColumn(
                name: "voided_date",
                table: "POSales",
                newName: "VoidedDate");

            migrationBuilder.RenameColumn(
                name: "voided_by",
                table: "POSales",
                newName: "VoidedBy");

            migrationBuilder.RenameColumn(
                name: "trip_ticket",
                table: "POSales",
                newName: "TripTicket");

            migrationBuilder.RenameColumn(
                name: "station_code",
                table: "POSales",
                newName: "StationCode");

            migrationBuilder.RenameColumn(
                name: "shift_rec_id",
                table: "POSales",
                newName: "ShiftRecId");

            migrationBuilder.RenameColumn(
                name: "shift_no",
                table: "POSales",
                newName: "ShiftNo");

            migrationBuilder.RenameColumn(
                name: "product_code",
                table: "POSales",
                newName: "ProductCode");

            migrationBuilder.RenameColumn(
                name: "posted_date",
                table: "POSales",
                newName: "PostedDate");

            migrationBuilder.RenameColumn(
                name: "posted_by",
                table: "POSales",
                newName: "PostedBy");

            migrationBuilder.RenameColumn(
                name: "po_sales_time",
                table: "POSales",
                newName: "POSalesTime");

            migrationBuilder.RenameColumn(
                name: "po_sales_no",
                table: "POSales",
                newName: "POSalesNo");

            migrationBuilder.RenameColumn(
                name: "po_sales_date",
                table: "POSales",
                newName: "POSalesDate");

            migrationBuilder.RenameColumn(
                name: "plate_no",
                table: "POSales",
                newName: "PlateNo");

            migrationBuilder.RenameColumn(
                name: "edited_date",
                table: "POSales",
                newName: "EditedDate");

            migrationBuilder.RenameColumn(
                name: "edited_by",
                table: "POSales",
                newName: "EditedBy");

            migrationBuilder.RenameColumn(
                name: "dr_no",
                table: "POSales",
                newName: "DrNo");

            migrationBuilder.RenameColumn(
                name: "customer_code",
                table: "POSales",
                newName: "CustomerCode");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "POSales",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "POSales",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "contract_price",
                table: "POSales",
                newName: "ContractPrice");

            migrationBuilder.RenameColumn(
                name: "cashier_code",
                table: "POSales",
                newName: "CashierCode");

            migrationBuilder.RenameColumn(
                name: "canceled_date",
                table: "POSales",
                newName: "CanceledDate");

            migrationBuilder.RenameColumn(
                name: "canceled_by",
                table: "POSales",
                newName: "CanceledBy");

            migrationBuilder.RenameColumn(
                name: "po_sales_id",
                table: "POSales",
                newName: "POSalesId");

            migrationBuilder.RenameIndex(
                name: "ix_po_sales_po_sales_no",
                table: "POSales",
                newName: "IX_POSales_POSalesNo");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "LubePurchaseHeaders",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "voided_date",
                table: "LubePurchaseHeaders",
                newName: "VoidedDate");

            migrationBuilder.RenameColumn(
                name: "voided_by",
                table: "LubePurchaseHeaders",
                newName: "VoidedBy");

            migrationBuilder.RenameColumn(
                name: "vatable_sales",
                table: "LubePurchaseHeaders",
                newName: "VatableSales");

            migrationBuilder.RenameColumn(
                name: "vat_amount",
                table: "LubePurchaseHeaders",
                newName: "VatAmount");

            migrationBuilder.RenameColumn(
                name: "supplier_code",
                table: "LubePurchaseHeaders",
                newName: "SupplierCode");

            migrationBuilder.RenameColumn(
                name: "station_code",
                table: "LubePurchaseHeaders",
                newName: "StationCode");

            migrationBuilder.RenameColumn(
                name: "shift_rec_id",
                table: "LubePurchaseHeaders",
                newName: "ShiftRecId");

            migrationBuilder.RenameColumn(
                name: "shift_no",
                table: "LubePurchaseHeaders",
                newName: "ShiftNo");

            migrationBuilder.RenameColumn(
                name: "sales_invoice",
                table: "LubePurchaseHeaders",
                newName: "SalesInvoice");

            migrationBuilder.RenameColumn(
                name: "received_by",
                table: "LubePurchaseHeaders",
                newName: "ReceivedBy");

            migrationBuilder.RenameColumn(
                name: "posted_date",
                table: "LubePurchaseHeaders",
                newName: "PostedDate");

            migrationBuilder.RenameColumn(
                name: "posted_by",
                table: "LubePurchaseHeaders",
                newName: "PostedBy");

            migrationBuilder.RenameColumn(
                name: "po_no",
                table: "LubePurchaseHeaders",
                newName: "PoNo");

            migrationBuilder.RenameColumn(
                name: "lube_purchase_header_no",
                table: "LubePurchaseHeaders",
                newName: "LubePurchaseHeaderNo");

            migrationBuilder.RenameColumn(
                name: "edited_date",
                table: "LubePurchaseHeaders",
                newName: "EditedDate");

            migrationBuilder.RenameColumn(
                name: "edited_by",
                table: "LubePurchaseHeaders",
                newName: "EditedBy");

            migrationBuilder.RenameColumn(
                name: "dr_no",
                table: "LubePurchaseHeaders",
                newName: "DrNo");

            migrationBuilder.RenameColumn(
                name: "detail_link",
                table: "LubePurchaseHeaders",
                newName: "DetailLink");

            migrationBuilder.RenameColumn(
                name: "delivery_date",
                table: "LubePurchaseHeaders",
                newName: "DeliveryDate");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "LubePurchaseHeaders",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "LubePurchaseHeaders",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "cashier_code",
                table: "LubePurchaseHeaders",
                newName: "CashierCode");

            migrationBuilder.RenameColumn(
                name: "canceled_date",
                table: "LubePurchaseHeaders",
                newName: "CanceledDate");

            migrationBuilder.RenameColumn(
                name: "canceled_by",
                table: "LubePurchaseHeaders",
                newName: "CanceledBy");

            migrationBuilder.RenameColumn(
                name: "lube_purchase_header_id",
                table: "LubePurchaseHeaders",
                newName: "LubePurchaseHeaderId");

            migrationBuilder.RenameIndex(
                name: "ix_lube_purchase_headers_station_code",
                table: "LubePurchaseHeaders",
                newName: "IX_LubePurchaseHeaders_StationCode");

            migrationBuilder.RenameIndex(
                name: "ix_lube_purchase_headers_lube_purchase_header_no",
                table: "LubePurchaseHeaders",
                newName: "IX_LubePurchaseHeaders_LubePurchaseHeaderNo");

            migrationBuilder.RenameColumn(
                name: "unit",
                table: "LubePurchaseDetails",
                newName: "Unit");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "LubePurchaseDetails",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "piece",
                table: "LubePurchaseDetails",
                newName: "Piece");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "LubePurchaseDetails",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "LubePurchaseDetails",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "station_code",
                table: "LubePurchaseDetails",
                newName: "StationCode");

            migrationBuilder.RenameColumn(
                name: "product_code",
                table: "LubePurchaseDetails",
                newName: "ProductCode");

            migrationBuilder.RenameColumn(
                name: "lube_purchase_header_no",
                table: "LubePurchaseDetails",
                newName: "LubePurchaseHeaderNo");

            migrationBuilder.RenameColumn(
                name: "lube_purchase_header_id",
                table: "LubePurchaseDetails",
                newName: "LubePurchaseHeaderId");

            migrationBuilder.RenameColumn(
                name: "cost_per_piece",
                table: "LubePurchaseDetails",
                newName: "CostPerPiece");

            migrationBuilder.RenameColumn(
                name: "cost_per_case",
                table: "LubePurchaseDetails",
                newName: "CostPerCase");

            migrationBuilder.RenameColumn(
                name: "lube_purchase_detail_id",
                table: "LubePurchaseDetails",
                newName: "LubePurchaseDetailId");

            migrationBuilder.RenameIndex(
                name: "ix_lube_purchase_details_product_code",
                table: "LubePurchaseDetails",
                newName: "IX_LubePurchaseDetails_ProductCode");

            migrationBuilder.RenameIndex(
                name: "ix_lube_purchase_details_lube_purchase_header_no",
                table: "LubePurchaseDetails",
                newName: "IX_LubePurchaseDetails_LubePurchaseHeaderNo");

            migrationBuilder.RenameIndex(
                name: "ix_lube_purchase_details_lube_purchase_header_id",
                table: "LubePurchaseDetails",
                newName: "IX_LubePurchaseDetails_LubePurchaseHeaderId");

            migrationBuilder.RenameColumn(
                name: "lube_delivery_id",
                table: "LubeDeliveries",
                newName: "LubeDeliveryId");

            migrationBuilder.RenameIndex(
                name: "ix_lube_deliveries_stncode",
                table: "LubeDeliveries",
                newName: "IX_LubeDeliveries_stncode");

            migrationBuilder.RenameIndex(
                name: "ix_lube_deliveries_shiftrecid",
                table: "LubeDeliveries",
                newName: "IX_LubeDeliveries_shiftrecid");

            migrationBuilder.RenameIndex(
                name: "ix_lube_deliveries_dtllink",
                table: "LubeDeliveries",
                newName: "IX_LubeDeliveries_dtllink");

            migrationBuilder.RenameColumn(
                name: "message",
                table: "LogMessages",
                newName: "Message");

            migrationBuilder.RenameColumn(
                name: "time_stamp",
                table: "LogMessages",
                newName: "TimeStamp");

            migrationBuilder.RenameColumn(
                name: "logger_name",
                table: "LogMessages",
                newName: "LoggerName");

            migrationBuilder.RenameColumn(
                name: "log_level",
                table: "LogMessages",
                newName: "LogLevel");

            migrationBuilder.RenameColumn(
                name: "log_id",
                table: "LogMessages",
                newName: "LogId");

            migrationBuilder.RenameColumn(
                name: "reference",
                table: "GeneralLedgers",
                newName: "Reference");

            migrationBuilder.RenameColumn(
                name: "particular",
                table: "GeneralLedgers",
                newName: "Particular");

            migrationBuilder.RenameColumn(
                name: "debit",
                table: "GeneralLedgers",
                newName: "Debit");

            migrationBuilder.RenameColumn(
                name: "credit",
                table: "GeneralLedgers",
                newName: "Credit");

            migrationBuilder.RenameColumn(
                name: "transaction_date",
                table: "GeneralLedgers",
                newName: "TransactionDate");

            migrationBuilder.RenameColumn(
                name: "supplier_code",
                table: "GeneralLedgers",
                newName: "SupplierCode");

            migrationBuilder.RenameColumn(
                name: "station_code",
                table: "GeneralLedgers",
                newName: "StationCode");

            migrationBuilder.RenameColumn(
                name: "product_code",
                table: "GeneralLedgers",
                newName: "ProductCode");

            migrationBuilder.RenameColumn(
                name: "journal_reference",
                table: "GeneralLedgers",
                newName: "JournalReference");

            migrationBuilder.RenameColumn(
                name: "is_validated",
                table: "GeneralLedgers",
                newName: "IsValidated");

            migrationBuilder.RenameColumn(
                name: "customer_code",
                table: "GeneralLedgers",
                newName: "CustomerCode");

            migrationBuilder.RenameColumn(
                name: "account_title",
                table: "GeneralLedgers",
                newName: "AccountTitle");

            migrationBuilder.RenameColumn(
                name: "account_number",
                table: "GeneralLedgers",
                newName: "AccountNumber");

            migrationBuilder.RenameColumn(
                name: "general_ledger_id",
                table: "GeneralLedgers",
                newName: "GeneralLedgerId");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_transaction_date",
                table: "GeneralLedgers",
                newName: "IX_GeneralLedgers_TransactionDate");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_supplier_code",
                table: "GeneralLedgers",
                newName: "IX_GeneralLedgers_SupplierCode");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_station_code",
                table: "GeneralLedgers",
                newName: "IX_GeneralLedgers_StationCode");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_reference",
                table: "GeneralLedgers",
                newName: "IX_GeneralLedgers_Reference");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_product_code",
                table: "GeneralLedgers",
                newName: "IX_GeneralLedgers_ProductCode");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_journal_reference",
                table: "GeneralLedgers",
                newName: "IX_GeneralLedgers_JournalReference");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_customer_code",
                table: "GeneralLedgers",
                newName: "IX_GeneralLedgers_CustomerCode");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_account_title",
                table: "GeneralLedgers",
                newName: "IX_GeneralLedgers_AccountTitle");

            migrationBuilder.RenameIndex(
                name: "ix_general_ledgers_account_number",
                table: "GeneralLedgers",
                newName: "IX_GeneralLedgers_AccountNumber");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "FuelPurchase",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "hauler",
                table: "FuelPurchase",
                newName: "Hauler");

            migrationBuilder.RenameColumn(
                name: "driver",
                table: "FuelPurchase",
                newName: "Driver");

            migrationBuilder.RenameColumn(
                name: "wc_no",
                table: "FuelPurchase",
                newName: "WcNo");

            migrationBuilder.RenameColumn(
                name: "voided_date",
                table: "FuelPurchase",
                newName: "VoidedDate");

            migrationBuilder.RenameColumn(
                name: "voided_by",
                table: "FuelPurchase",
                newName: "VoidedBy");

            migrationBuilder.RenameColumn(
                name: "time_out",
                table: "FuelPurchase",
                newName: "TimeOut");

            migrationBuilder.RenameColumn(
                name: "time_in",
                table: "FuelPurchase",
                newName: "TimeIn");

            migrationBuilder.RenameColumn(
                name: "tank_no",
                table: "FuelPurchase",
                newName: "TankNo");

            migrationBuilder.RenameColumn(
                name: "station_code",
                table: "FuelPurchase",
                newName: "StationCode");

            migrationBuilder.RenameColumn(
                name: "should_be",
                table: "FuelPurchase",
                newName: "ShouldBe");

            migrationBuilder.RenameColumn(
                name: "shift_rec_id",
                table: "FuelPurchase",
                newName: "ShiftRecId");

            migrationBuilder.RenameColumn(
                name: "shift_no",
                table: "FuelPurchase",
                newName: "ShiftNo");

            migrationBuilder.RenameColumn(
                name: "selling_price",
                table: "FuelPurchase",
                newName: "SellingPrice");

            migrationBuilder.RenameColumn(
                name: "received_by",
                table: "FuelPurchase",
                newName: "ReceivedBy");

            migrationBuilder.RenameColumn(
                name: "quantity_before",
                table: "FuelPurchase",
                newName: "QuantityBefore");

            migrationBuilder.RenameColumn(
                name: "quantity_after",
                table: "FuelPurchase",
                newName: "QuantityAfter");

            migrationBuilder.RenameColumn(
                name: "purchase_price",
                table: "FuelPurchase",
                newName: "PurchasePrice");

            migrationBuilder.RenameColumn(
                name: "product_code",
                table: "FuelPurchase",
                newName: "ProductCode");

            migrationBuilder.RenameColumn(
                name: "posted_date",
                table: "FuelPurchase",
                newName: "PostedDate");

            migrationBuilder.RenameColumn(
                name: "posted_by",
                table: "FuelPurchase",
                newName: "PostedBy");

            migrationBuilder.RenameColumn(
                name: "plate_no",
                table: "FuelPurchase",
                newName: "PlateNo");

            migrationBuilder.RenameColumn(
                name: "gain_or_loss",
                table: "FuelPurchase",
                newName: "GainOrLoss");

            migrationBuilder.RenameColumn(
                name: "fuel_purchase_no",
                table: "FuelPurchase",
                newName: "FuelPurchaseNo");

            migrationBuilder.RenameColumn(
                name: "edited_date",
                table: "FuelPurchase",
                newName: "EditedDate");

            migrationBuilder.RenameColumn(
                name: "edited_by",
                table: "FuelPurchase",
                newName: "EditedBy");

            migrationBuilder.RenameColumn(
                name: "dr_no",
                table: "FuelPurchase",
                newName: "DrNo");

            migrationBuilder.RenameColumn(
                name: "delivery_date",
                table: "FuelPurchase",
                newName: "DeliveryDate");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "FuelPurchase",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "FuelPurchase",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "cashier_code",
                table: "FuelPurchase",
                newName: "CashierCode");

            migrationBuilder.RenameColumn(
                name: "canceled_date",
                table: "FuelPurchase",
                newName: "CanceledDate");

            migrationBuilder.RenameColumn(
                name: "canceled_by",
                table: "FuelPurchase",
                newName: "CanceledBy");

            migrationBuilder.RenameColumn(
                name: "fuel_purchase_id",
                table: "FuelPurchase",
                newName: "FuelPurchaseId");

            migrationBuilder.RenameIndex(
                name: "ix_fuel_purchase_station_code",
                table: "FuelPurchase",
                newName: "IX_FuelPurchase_StationCode");

            migrationBuilder.RenameIndex(
                name: "ix_fuel_purchase_product_code",
                table: "FuelPurchase",
                newName: "IX_FuelPurchase_ProductCode");

            migrationBuilder.RenameIndex(
                name: "ix_fuel_purchase_fuel_purchase_no",
                table: "FuelPurchase",
                newName: "IX_FuelPurchase_FuelPurchaseNo");

            migrationBuilder.RenameColumn(
                name: "fuel_delivery_id",
                table: "FuelDeliveries",
                newName: "FuelDeliveryId");

            migrationBuilder.RenameIndex(
                name: "ix_fuel_deliveries_stncode",
                table: "FuelDeliveries",
                newName: "IX_FuelDeliveries_stncode");

            migrationBuilder.RenameIndex(
                name: "ix_fuel_deliveries_shiftrecid",
                table: "FuelDeliveries",
                newName: "IX_FuelDeliveries_shiftrecid");

            migrationBuilder.RenameColumn(
                name: "station_code",
                table: "CsvFiles",
                newName: "StationCode");

            migrationBuilder.RenameColumn(
                name: "is_uploaded",
                table: "CsvFiles",
                newName: "IsUploaded");

            migrationBuilder.RenameColumn(
                name: "file_name",
                table: "CsvFiles",
                newName: "FileName");

            migrationBuilder.RenameColumn(
                name: "file_id",
                table: "CsvFiles",
                newName: "FileId");

            migrationBuilder.RenameColumn(
                name: "parent",
                table: "ChartOfAccounts",
                newName: "Parent");

            migrationBuilder.RenameColumn(
                name: "level",
                table: "ChartOfAccounts",
                newName: "Level");

            migrationBuilder.RenameColumn(
                name: "normal_balance",
                table: "ChartOfAccounts",
                newName: "NormalBalance");

            migrationBuilder.RenameColumn(
                name: "is_main",
                table: "ChartOfAccounts",
                newName: "IsMain");

            migrationBuilder.RenameColumn(
                name: "edited_date",
                table: "ChartOfAccounts",
                newName: "EditedDate");

            migrationBuilder.RenameColumn(
                name: "edited_by",
                table: "ChartOfAccounts",
                newName: "EditedBy");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "ChartOfAccounts",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "ChartOfAccounts",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "account_type",
                table: "ChartOfAccounts",
                newName: "AccountType");

            migrationBuilder.RenameColumn(
                name: "account_number",
                table: "ChartOfAccounts",
                newName: "AccountNumber");

            migrationBuilder.RenameColumn(
                name: "account_name",
                table: "ChartOfAccounts",
                newName: "AccountName");

            migrationBuilder.RenameColumn(
                name: "account_id",
                table: "ChartOfAccounts",
                newName: "AccountId");

            migrationBuilder.RenameIndex(
                name: "ix_chart_of_accounts_account_number",
                table: "ChartOfAccounts",
                newName: "IX_ChartOfAccounts_AccountNumber");

            migrationBuilder.RenameIndex(
                name: "ix_chart_of_accounts_account_name",
                table: "ChartOfAccounts",
                newName: "IX_ChartOfAccounts_AccountName");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers",
                column: "SupplierId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Stations",
                table: "Stations",
                column: "StationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Products",
                table: "Products",
                column: "ProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Offlines",
                table: "Offlines",
                column: "OfflineId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Lubes",
                table: "Lubes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Inventories",
                table: "Inventories",
                column: "InventoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Fuels",
                table: "Fuels",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Customers",
                table: "Customers",
                column: "CustomerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Companies",
                table: "Companies",
                column: "CompanyId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesHeaders",
                table: "SalesHeaders",
                column: "SalesHeaderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesDetails",
                table: "SalesDetails",
                column: "SalesDetailId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SafeDrops",
                table: "SafeDrops",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PoSalesRaw",
                table: "PoSalesRaw",
                column: "POSalesRawId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_POSales",
                table: "POSales",
                column: "POSalesId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LubePurchaseHeaders",
                table: "LubePurchaseHeaders",
                column: "LubePurchaseHeaderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LubePurchaseDetails",
                table: "LubePurchaseDetails",
                column: "LubePurchaseDetailId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LubeDeliveries",
                table: "LubeDeliveries",
                column: "LubeDeliveryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LogMessages",
                table: "LogMessages",
                column: "LogId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GeneralLedgers",
                table: "GeneralLedgers",
                column: "GeneralLedgerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FuelPurchase",
                table: "FuelPurchase",
                column: "FuelPurchaseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FuelDeliveries",
                table: "FuelDeliveries",
                column: "FuelDeliveryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CsvFiles",
                table: "CsvFiles",
                column: "FileId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChartOfAccounts",
                table: "ChartOfAccounts",
                column: "AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LubePurchaseDetails_LubePurchaseHeaders_LubePurchaseHeaderId",
                table: "LubePurchaseDetails",
                column: "LubePurchaseHeaderId",
                principalTable: "LubePurchaseHeaders",
                principalColumn: "LubePurchaseHeaderId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesDetails_SalesHeaders_SalesHeaderId",
                table: "SalesDetails",
                column: "SalesHeaderId",
                principalTable: "SalesHeaders",
                principalColumn: "SalesHeaderId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
