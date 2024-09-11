﻿using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class CustomerOrderSlipAppointingHauler
    {
        public int CustomerOrderSlipId { get; set; }

        public int HaulerId { get; set; }

        public List<SelectListItem>? Haulers { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Freight { get; set; }

        public string CurrentUser { get; set; } = string.Empty;
    }
}
