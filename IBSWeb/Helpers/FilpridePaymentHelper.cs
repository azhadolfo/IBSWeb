using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Integrated;
using IBS.Utility;
using IBS.Utility.Constants;

namespace IBSWeb.Helpers
{
    /// <summary>
    /// Centralized payment calculation helper for Filpride RRs and DRs.
    /// Ensures consistent calculation across all controller operations (Post, Cancel, Void, Unpost).
    /// </summary>
    public static class FilpridePaymentHelper
    {
        #region RR Payment Calculations

        /// <summary>
        /// Calculates the remaining balance for a Receiving Report.
        /// Uses tax-adjusted calculation as per business requirements.
        /// </summary>
        public static decimal GetRRRemainingBalance(FilprideReceivingReport rr, IReceivingReportRepository receivingReportRepo)
        {
            var netOfVatAmount = rr.PurchaseOrder?.VatType == SD.VatType_Vatable
                ? receivingReportRepo.ComputeNetOfVat(rr.Amount)
                : rr.Amount;

            var ewtAmount = rr.PurchaseOrder?.TaxType == SD.TaxType_WithTax
                ? receivingReportRepo.ComputeEwtAmount(netOfVatAmount, rr.TaxPercentage)
                : 0.0000m;

            var netOfEwtAmount = rr.PurchaseOrder?.TaxType == SD.TaxType_WithTax
                ? receivingReportRepo.ComputeNetOfEwt(rr.Amount, ewtAmount)
                : rr.Amount;

            return netOfEwtAmount - rr.AmountPaid;
        }

        /// <summary>
        /// Determines if a Receiving Report is fully paid based on tax-adjusted calculation.
        /// </summary>
        public static bool IsRRFullyPaid(FilprideReceivingReport rr, IReceivingReportRepository receivingReportRepo)
        {
            var remainingBalance = GetRRRemainingBalance(rr, receivingReportRepo);
            return remainingBalance <= 0;
        }

        /// <summary>
        /// Calculates the tax-adjusted amount for comparison (used in filtering).
        /// </summary>
        public static decimal GetRRAdjustedAmount(FilprideReceivingReport rr, IReceivingReportRepository receivingReportRepo)
        {
            var netOfVatAmount = rr.PurchaseOrder?.VatType == SD.VatType_Vatable
                ? receivingReportRepo.ComputeNetOfVat(rr.Amount)
                : rr.Amount;

            var ewtAmount = rr.PurchaseOrder?.TaxType == SD.TaxType_WithTax
                ? receivingReportRepo.ComputeEwtAmount(netOfVatAmount, rr.TaxPercentage)
                : 0.0000m;

            return rr.PurchaseOrder?.TaxType == SD.TaxType_WithTax
                ? receivingReportRepo.ComputeNetOfEwt(rr.Amount, ewtAmount)
                : rr.Amount;
        }

        #endregion

        #region DR Commission Payment Calculations

        /// <summary>
        /// Calculates the remaining commission for a Delivery Receipt.
        /// </summary>
        public static decimal GetCommissionRemaining(FilprideDeliveryReceipt dr, IReceivingReportRepository receivingReportRepo)
        {
            var netOfVatAmount = dr.CustomerOrderSlip?.CommissioneeVatType == SD.VatType_Vatable
                ? receivingReportRepo.ComputeNetOfVat(dr.CommissionAmount)
                : dr.CommissionAmount;

            var ewtAmount = dr.CustomerOrderSlip?.CommissioneeTaxType == SD.TaxType_WithTax
                ? receivingReportRepo.ComputeEwtAmount(netOfVatAmount, dr.Commissionee?.WithholdingTaxPercent ?? 0m)
                : 0m;

            var netOfEwtAmount = dr.CustomerOrderSlip?.CommissioneeTaxType == SD.TaxType_WithTax
                ? receivingReportRepo.ComputeNetOfEwt(dr.CommissionAmount, ewtAmount)
                : dr.CommissionAmount;

            return netOfEwtAmount - dr.CommissionAmountPaid;
        }

        /// <summary>
        /// Determines if commission is fully paid for a Delivery Receipt.
        /// </summary>
        public static bool IsCommissionFullyPaid(FilprideDeliveryReceipt dr, IReceivingReportRepository receivingReportRepo)
        {
            var remainingBalance = GetCommissionRemaining(dr, receivingReportRepo);
            return remainingBalance <= 0;
        }

        #endregion

        #region DR Freight Payment Calculations

        /// <summary>
        /// Calculates the remaining freight for a Delivery Receipt.
        /// </summary>
        public static decimal GetFreightRemaining(FilprideDeliveryReceipt dr, IReceivingReportRepository receivingReportRepo)
        {
            var netOfVatAmount = dr.HaulerVatType == SD.VatType_Vatable
                ? receivingReportRepo.ComputeNetOfVat(dr.FreightAmount)
                : dr.FreightAmount;

            var ewtAmount = dr.HaulerTaxType == SD.TaxType_WithTax
                ? receivingReportRepo.ComputeEwtAmount(netOfVatAmount, dr.Hauler?.WithholdingTaxPercent ?? 0m)
                : 0m;

            var netOfEwtAmount = dr.HaulerTaxType == SD.TaxType_WithTax
                ? receivingReportRepo.ComputeNetOfEwt(dr.FreightAmount, ewtAmount)
                : dr.FreightAmount;

            return netOfEwtAmount - dr.FreightAmountPaid;
        }

        /// <summary>
        /// Determines if freight is fully paid for a Delivery Receipt.
        /// </summary>
        public static bool IsFreightFullyPaid(FilprideDeliveryReceipt dr, IReceivingReportRepository receivingReportRepo)
        {
            var remainingBalance = GetFreightRemaining(dr, receivingReportRepo);
            return remainingBalance <= 0;
        }

        #endregion
    }
}
