namespace IBS.Utility.Msap
{
    public static class SD
    {
        public const string Company_Filpride = "Filpride";
        public const string Company_MMSI = "MMSI";

        #region Terms

        public const string Terms_Cod = "COD";
        public const string Terms_Prepaid = "PREPAID";
        public const string Terms_7d = "7D";
        public const string Terms_10d = "10D";
        public const string Terms_15d = "15D";
        public const string Terms_20d = "20D";
        public const string Terms_21d = "21D";
        public const string Terms_30d = "30D";
        public const string Terms_45d = "45D";
        public const string Terms_60d = "60D";
        public const string Terms_90d = "90D";
        public const string Terms_7pdc = "7PDC";
        public const string Terms_15pdc = "15PDC";
        public const string Terms_30pdc = "30PDC";
        public const string Terms_45pdc = "45PDC";
        public const string Terms_60pdc = "60PDC";
        public const string Terms_M15 = "M15";
        public const string Terms_M29 = "M29";
        public const string Terms_M30 = "M30";

        #endregion Terms

        #region Vat Type

        public const string VatType_Vatable = "Vatable";
        public const string VatType_ZeroRated = "Zero-Rated";
        public const string VatType_Exempt = "Exempt";

        #endregion Vat Type

        #region Tax Type

        public const string TaxType_WithTax = "Withholding Tax";
        public const string TaxType_WithVat = "Withholding Vat";
        public const string TaxType_Exempt = "Exempt";

        #endregion Tax Type

        #region Delivery Option

        public const string DeliveryOption_ForPickUpByClient = "For Pick Up By Client";
        public const string DeliveryOption_DirectDelivery = "Direct Delivery";
        public const string DeliveryOption_ForPickUpByHauler = "For Pick Up By Hauler";

        #endregion Delivery Option

        #region Filpride Department

        public const string Department_Accounting = "Accounting";
        public const string Department_CreditAndCollection = "Credit and Collection";
        public const string Department_Engineering = "Engineering";
        public const string Department_Finance = "Finance";
        public const string Department_HRAndAdminOrLegal = "HR and Admin/Legal";
        public const string Department_Logistics = "Logistics";
        public const string Department_Marketing = "Marketing";
        public const string Department_ManagementAccounting = "Management Accounting";
        public const string Department_MIS = "MIS";
        public const string Department_Operation = "Operation";
        public const string Department_RCD = "RCD";
        public const string Department_RetailAdmin = "Retail Admin";
        public const string Department_RetailAudit = "Retail Audit";
        public const string Department_SiteDevAndSiteAcquisition = "Site Dev and Acquisition";
        public const string Department_StationCashier = "Station Cashier";
        public const string Department_TradeAndSupply = "Trade and Supply";
        public const string Department_TrainingAndCompliance = "Training and Compliance";

        public const string CustomerType_PO = "PO";

        #endregion Filpride Department

        #region Filpride Position

        public const string Position_OperationManager = "Operation Manager";
        public const string Position_FinanceManager = "Finance Manager";

        #endregion Filpride Position

        #region Format string

        public const string Two_Decimal_Format = "N2";
        public const string Four_Decimal_Format = "N4";
        public const string Date_Format = "MMM dd, yyyy";

        #endregion Format string

        public static class JobOrderStatus
        {
            public const string Open = "Open";
            public const string Closed = "Closed";
        }

        public static class ServiceRequestStatus
        {
            public const string Draft = "Draft";
            public const string Requested = "Requested";
            public const string ServiceRequestDeleted = "Service Request Deleted";

            public static readonly string[] All = [Draft, Requested, ServiceRequestDeleted];
        }

        public static class DispatchTicketStatus
        {
            public const string ForTariff = "For Tariff";
            public const string ForApproval = "For Approval";
            public const string Disapproved = "Disapproved";
            public const string ForBilling = "For Billing";
            public const string Billed = "Billed";
            public const string Deleted = "Deleted";

            public static readonly string[] All = [ForTariff, ForApproval, Disapproved, ForBilling, Billed, Deleted];
        }

        public static class BillingStatus
        {
            public const string ForPosting = "For Posting";
            public const string ForCollection = "For Collection";
            public const string Collected = "Collected";
        }

        public static class VesselScheduleStatus
        {
            public const string Tentative = "Tentative";
            public const string Confirmed = "Confirmed";
            public const string InProgress = "In Progress";
            public const string Completed = "Completed";
            public const string Cancelled = "Cancelled";
        }

        public const string BilledToLocal = "LOCAL";
        public const string BilledToForeign = "FOREIGN";

        #region MSAP Accounting Accounts

        public static class MsapAccounts
        {
            public const string CashInBank = "101010100";
            public const string ArTrade = "101020100";
            public const string ArTradeCwt = "101020200";
            public const string ArTradeCwv = "101020300";
            public const string Cwt = "101060400";
            public const string Cwv = "101060600";
            public const string OutputVat = "201030100";
            public const string MaritimeServiceRevenue = "401020100";
        }
        #endregion

    }
}
