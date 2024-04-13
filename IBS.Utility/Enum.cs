using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public enum AccountCategory
    {
        Debit,
        Credit
    }

}