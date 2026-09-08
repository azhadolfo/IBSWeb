namespace IBS.DTOs.Msap
{
    public class BatchTariffRequest
    {
        public List<int> Ids { get; set; } = [];
        public decimal DispatchRate { get; set; }
        public decimal BafRate { get; set; }
        public string ChargeType { get; set; } = "Per hour";
        public string ChargeType2 { get; set; } = "Per hour";
    }
}
