﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models.ViewModels
{
    public class SalesVM
    {
        public SalesHeader Header { get; set; }
        public IEnumerable<SalesDetail> Details { get; set; }
    }
}