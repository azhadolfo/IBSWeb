﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models
{
    public class LubePurchaseHeader : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LubePurchaseHeaderId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string LubePurchaseHeaderNo { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string ShiftRecId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string DetailLink { get; set; }

        [Column(TypeName = "varchar(5)")]
        [Display(Name = "Station Code")]
        public string StationCode { get; set; }

        [Column(TypeName = "varchar(5)")]
        public string EmployeeNo { get; set; }

        [Display(Name = "Shift No")]
        public int ShiftNo { get; set; }

        [Column(TypeName = "date")]
        [Display(Name = "Delivery Date")]
        public DateOnly DeliveryDate { get; set; }

        [Column(TypeName = "varchar(50)")]
        [Display(Name = "Sales Invoice")]
        public string SalesInvoice { get; set; }

        [Column(TypeName = "varchar(10)")]
        [Display(Name = "Supplier Code")]
        public string SupplierCode { get; set; }

        [Column(TypeName = "varchar(50)")]
        [Display(Name = "DR#")]
        public string DrNo { get; set; }

        [Column(TypeName = "varchar(50)")]
        [Display(Name = "PO#")]
        public string PoNo { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal VatableSales { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal VatAmount { get; set; }

        [Column(TypeName = "varchar(50)")]
        [Display(Name = "Received By")]
        public string ReceivedBy { get; set; }

    }
}
