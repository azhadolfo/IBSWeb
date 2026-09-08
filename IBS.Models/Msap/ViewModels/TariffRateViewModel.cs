namespace IBS.Models.Msap.ViewModels
{
    public class TariffRateViewModel : TariffRate
    {
        public TariffRateViewModel() { }

        public TariffRateViewModel(TariffRate entity)
        {
            TariffRateId = entity.TariffRateId;
            AsOfDate = entity.AsOfDate;
            CustomerId = entity.CustomerId;
            PortId = entity.PortId;
            TerminalId = entity.TerminalId;
            ServiceId = entity.ServiceId;
            Dispatch = entity.Dispatch;
            BAF = entity.BAF;
            CreatedBy = entity.CreatedBy;
            CreatedDate = entity.CreatedDate;
            UpdateBy = entity.UpdateBy;
            UpdateDate = entity.UpdateDate;
            DispatchDiscount = entity.DispatchDiscount;
            BAFDiscount = entity.BAFDiscount;
        }
    }
}
