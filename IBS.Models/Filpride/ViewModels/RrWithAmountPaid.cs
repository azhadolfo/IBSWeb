using IBS.Models.Filpride.Integrated;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IBS.Models.Filpride.AccountsPayable;

namespace IBS.Models.Filpride
{
    public class RrWithAmountPaid
    {
        public FilprideReceivingReport ReceivingReport { get; set; } = null!;
        public decimal AmountPaid { get; set; }
    }
}
