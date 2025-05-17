using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI.ViewModels
{
    public class TariffViewModel
    {
        public int DispatchTicketId { get; set; }

        public int? CustomerId { get; set; }

        public decimal DispatchRate { get; set; }

        public decimal DispatchDiscount { get; set; }

        public decimal DispatchBillingAmount { get; set; }

        public decimal DispatchNetRevenue { get; set; }

        public decimal BAFRate { get; set; }

        public decimal BAFDiscount { get; set; }

        public decimal BAFBillingAmount { get; set; }

        public decimal BAFNetRevenue { get; set; }

        public decimal TotalBilling { get; set; }

        public decimal TotalNetRevenue { get; set; }

        public decimal? ApOtherTugs { get; set; }

        #region --For showing only

        public string? DispatchNumber { get; set; }

        public string? COSNumber { get; set; }

        public string? VoyageNumber { get; set; }

        public DateOnly? Date { get; set; }

        public string? TugMasterName { get; set; }

        public DateOnly? DateLeft { get; set; }

        public TimeOnly? TimeLeft { get; set; }

        public DateOnly? DateArrived { get; set; }

        public TimeOnly? TimeArrived { get; set; }

        public string? TugboatName { get; set; }

        public string? VesselName { get; set; }

        public string? VesselType { get; set; }

        public string? TerminalName { get; set; }

        public string? PortName { get; set; }

        public bool? IsTugboatCompanyOwned  { get; set; }

        public string? TugboatOwnerName { get; set; }

        public decimal? FixedRate { get; set; }

        public string? Remarks { get; set; }

        public decimal? TotalHours { get; set; }

        public string? ImageName { get; set; }

        public string? CustomerName { get; set; }

        public string? DispatchChargeType { get; set; }

        public string? BAFChargeType { get; set; }

        #endregion

        #region --Select List

        public List<SelectListItem>? Customers { get; set; }

        #endregion

    }
}
