using IBS.Utility.Helpers;

namespace IBS.Services
{
    public static class TradeVoucherDisplayCalculator
    {
        public static (decimal BaseAmount, decimal InputVat, Dictionary<string, decimal> WithholdingByAccount) Calculate(
            IEnumerable<(decimal GrossAmount, bool IsVatable, bool IsTaxable, decimal TaxPercent, decimal PaymentAmount)> documents)
        {
            decimal totalBase = 0m, totalVat = 0m;
            var withholdingByAccount = new Dictionary<string, decimal>();

            foreach (var document in documents)
            {
                var (gross, isVatable, isTaxable, taxPercent, payment) = document;
                if (gross <= 0m || taxPercent < 0m || payment <= 0m || payment != DecimalRoundingHelper.RoundToFour(payment))
                {
                    throw new ArgumentException("Document amounts and payment allocations must be positive and payments must have at most four decimal places.");
                }

                var baseAmount = isVatable ? DecimalRoundingHelper.ComputeNetOfVat(gross) : gross;
                var vat = isVatable ? DecimalRoundingHelper.ComputeVatAmount(baseAmount) : 0m;
                var ewt = isTaxable ? DecimalRoundingHelper.ComputeEwtAmount(baseAmount, taxPercent) : 0m;
                var netPayable = DecimalRoundingHelper.RoundToFour(gross - ewt);
                if (netPayable <= 0m || payment > netPayable)
                {
                    throw new ArgumentException("Payment allocation cannot exceed the document's net payable amount.");
                }

                var withholdingAccount = ewt != 0m
                    ? WithholdingTaxHelper.GetAccountNumberByPercent(taxPercent)
                      ?? throw new ArgumentException($"No EWT account mapping found for tax percentage '{taxPercent}'.")
                    : null;

                if (payment != netPayable)
                {
                    var ratio = payment / netPayable;
                    baseAmount = DecimalRoundingHelper.RoundToFour(baseAmount * ratio);
                    vat = DecimalRoundingHelper.RoundToFour(vat * ratio);
                    ewt = DecimalRoundingHelper.RoundToFour(ewt * ratio);
                }

                totalBase += baseAmount;
                totalVat += vat;
                if (withholdingAccount != null && ewt != 0m)
                {
                    withholdingByAccount[withholdingAccount] = withholdingByAccount.GetValueOrDefault(withholdingAccount) + ewt;
                }
            }

            return (totalBase, totalVat, withholdingByAccount);
        }
    }
}
