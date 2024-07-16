﻿using IBS.Models.MasterFile;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride
{
    public class FilprideDeliveryReceipt : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DeliveryReceiptId { get; set; }

        [Column(TypeName = "varchar(12)")]
        [Display(Name = "DR No")]
        public string DeliveryReceiptNo { get; set; }

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM/dd/yyyy}")]
        public DateOnly Date { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string InvoiceNo { get; set; }

        #region--Customer properties

        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        #endregion

        #region--COS properties

        public int CustomerOrderSlipId { get; set; }

        [ForeignKey(nameof(CustomerOrderSlipId))]
        public FilprideCustomerOrderSlip? CustomerOrderSlip { get; set; }

        #endregion

        #region--Hauler properties

        public int? HaulerId { get; set; }

        [ForeignKey(nameof(HaulerId))]
        public Hauler? Hauler { get; set; }

        #endregion

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal Freight { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string AuthorityToLoadNo { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Remarks { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal NetOfVatAmount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal VatAmount { get; set; }

        public bool IsPrinted { get; set; }

        [Column(TypeName = "varchar(15)")]
        public string DeliveryType { get; set; }
    }
}