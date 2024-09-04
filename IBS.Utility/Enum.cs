using System.ComponentModel.DataAnnotations;

namespace IBS.Utility
{
    public enum CustomerType
    {
        Retail,
        Industrial,
        Government
    }

    public enum JournalType
    {
        Sales,
        Purchase,
        Inventory
    }

    public enum NormalBalance
    {
        Debit,
        Credit
    }

    public enum Port
    {
        [Display(Name = "Ex Sariaya")]
        Ex_Sariaya,

        [Display(Name = "Ex Bataan")]
        Ex_Bataan,

        [Display(Name = "Ex Lemery")]
        Ex_Lemery,

        [Display(Name = "Ex Batangas")]
        Ex_Batangas,

        [Display(Name = "Ex Cebu")]
        Ex_Cebu,

        [Display(Name = "Ex Bacolod")]
        Ex_Bacolod,

        [Display(Name = "Ex Cadiz")]
        Ex_Cadiz,

        [Display(Name = "Ex Davao")]
        Ex_Davao,

        [Display(Name = "Ex CDO")]
        Ex_CDO,

        [Display(Name = "Ex Poro")]
        Ex_Poro,

        [Display(Name = "Ex Subic")]
        Ex_Subic,

        [Display(Name = "Ex ITTC")]
        Ex_ITTC,

        [Display(Name = "Ex SBTI")]
        Ex_SBTI,

        [Display(Name = "Ex Pasacao")]
        Ex_Pasacao,

        [Display(Name = "Ex Tacloban")]
        Ex_Tacloban,

        [Display(Name = "Ex Mabini")]
        Ex_Mabini,

        [Display(Name = "Ex Vispet")]
        Ex_Vispet,

        [Display(Name = "Ex Gensan")]
        Ex_Gensan,

        [Display(Name = "Ex Bangar")]
        Ex_Bangar,

        [Display(Name = "Ex MNL")]
        Ex_MNL,

        [Display(Name = "Ex Badoc")]
        Ex_Badoc,

        [Display(Name = "Ex San Pascual")]
        Ex_SanPascual
    }

    public enum SalesInvoiceType
    {
        Documented,
        Undocumented
    }

    public enum CosStatus
    {
        Created,
        SupplierAppointed,
        ApprovedByOpsManager,
        ApprovedByFinance,
        HaulerAppointed,
        Completed
    }

    public enum Status
    {
        Pending,
        Posted,
        Voided,
        Canceled
    }
}