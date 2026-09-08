namespace IBS.Models.Msap.Enums
{
    public enum ProcedureEnum
    {
        #region -- MSAP Workflow --

        CreateServiceRequest,
        PostServiceRequest,
        CreateDispatchTicket,
        EditDispatchTicket,
        DeleteDispatchTicket,
        SetTariff,
        ApproveTariff,
        CreateBilling,
        EditBilling,
        DeleteBilling,
        ReverseBilling,
        CreateCollection,
        CreateJobOrder,
        EditJobOrder,
        DeleteJobOrder,
        CloseJobOrder,

        #endregion -- MSAP Workflow --

        #region -- Treasury --

        AccessTreasury,
        CreateDisbursement,

        #endregion -- Treasury --

        #region -- Posting Period --

        ManagePostedPeriod,

        #endregion -- Posting Period --

        #region -- MSAP Import --

        ManageMsapImport,

        #endregion -- MSAP Import --

        #region -- Master Files --

        ManageMaritimeMasterFile,

        #endregion -- Master Files --

        #region -- Reports --

        ViewGeneralLedger,
        ViewInventoryReport,
        ViewMaritimeReport,

        #endregion -- Reports --
    }
}
