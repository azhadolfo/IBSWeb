using IBS.Models.Filpride.Integrated;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models.Filpride
{
    public class DrWithAmountPaid
    {
        public FilprideDeliveryReceipt DeliveryReceipt { get; set; } = null!;
        public decimal AmountPaid { get; set; }
    }

    public record MonthYear(int Year, int Month);
}
