namespace IBS.Models.Msap.Enums
{
    public enum CustomerType
    {
        Retail,
        Industrial,
        Reseller
    }

    public enum DynamicView
    {
        Customer,
        Supplier,
        ChartOfAccount
    }

    public enum ModuleType
    {
        Sales,
        Collection,
    }

    public enum SubAccountType
    {
        Customer = 1,
        Supplier = 2,
        Employee = 3,
        BankAccount = 4,
        Company = 5
    }

}
