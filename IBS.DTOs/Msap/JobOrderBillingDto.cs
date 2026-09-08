namespace IBS.DTOs.Msap
{
    public class JobOrderBillingDto
    {
        public JobOrderHeaderDto Header { get; set; } = new();
        public List<JobOrderTicketDto> Tickets { get; set; } = new();
    }

    public class JobOrderHeaderDto
    {
        public int VesselId { get; set; }

        public int PortId { get; set; }

        public int TerminalId { get; set; }

        public string? VoyageNumber { get; set; }

        public string? COSNumber { get; set; }

        public bool IsVatable { get; set; }

        public bool IsVatInclusive { get; set; }

        public bool PrintWht { get; set; }
    }

    public class JobOrderTicketDto
    {
        public int DispatchTicketId { get; set; }
        public string DispatchNo { get; set; } = string.Empty;
        public string Tugboat { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public decimal Duration { get; set; }
        public decimal DispatchRate { get; set; }
        public decimal DispatchAmount { get; set; }
        public decimal BAFRate { get; set; }
        public decimal BAFDiscount { get; set; }
        public string BAFChargeType { get; set; } = string.Empty;
        public decimal BAFAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Port { get; set; }
        public string? Terminal { get; set; }
    }
}
