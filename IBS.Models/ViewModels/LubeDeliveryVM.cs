using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models.ViewModels
{
    public class LubeDeliveryVM
    {
        public LubePurchaseHeader Header { get; set; }

        public IEnumerable<LubePurchaseDetail> Details { get; set; }

    }
}
