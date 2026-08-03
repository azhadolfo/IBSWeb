namespace IBS.Utility.Helpers;

public static class DecimalRoundingHelper
{
    private const decimal VatRate = 0.12m;

    public static decimal RoundToFour(decimal value)
    {
        return Math.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    public static decimal DivideOrZero(decimal dividend, decimal divisor)
    {
        return divisor == 0m ? 0m : RoundToFour(dividend / divisor);
    }

    public static decimal ComputeNetOfVat(decimal grossAmount)
    {
        return grossAmount == 0m ? 0m : DivideOrZero(grossAmount, 1 + VatRate);
    }

    public static decimal ComputeVatAmount(decimal netOfVatAmount)
    {
        return netOfVatAmount == 0m ? 0m : RoundToFour(netOfVatAmount * VatRate);
    }

    public static decimal ComputeEwtAmount(decimal netOfVatAmount, decimal percent)
    {
        return netOfVatAmount == 0m || percent == 0m ? 0m : RoundToFour(netOfVatAmount * percent);
    }

    public static decimal ComputeNetUnitValue(decimal grossAmount, decimal quantity)
    {
        return quantity == 0m ? 0m : RoundToFour(grossAmount / quantity / (1 + VatRate));
    }
}
