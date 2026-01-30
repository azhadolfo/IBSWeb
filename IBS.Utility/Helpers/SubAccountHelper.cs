using IBS.Models.Enums;

namespace IBS.Utility.Helpers
{
    public class SubAccountHelper
    {
        public static (SubAccountType? Type, int? Id) DetermineCvSubAccount(
            int? customerId,
            int? supplierId,
            int? employeeId,
            int? bankId,
            int? companyId)
        {
            if (customerId.HasValue)
                return (SubAccountType.Customer, customerId.Value);

            if (supplierId.HasValue)
                return (SubAccountType.Supplier, supplierId.Value);

            if (employeeId.HasValue)
                return (SubAccountType.Employee, employeeId.Value);

            if (bankId.HasValue)
                return (SubAccountType.BankAccount, bankId.Value);

            if (companyId.HasValue)
                return (SubAccountType.Company, companyId.Value);

            return (null, null);
        }
    }
}
