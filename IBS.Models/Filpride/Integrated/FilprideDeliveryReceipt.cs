﻿using IBS.Models.Filpride.MasterFile;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.Integrated
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
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly Date { get; set; }

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly EstimatedTimeOfArrival { get; set; }

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly? DeliveredDate { get; set; }

        #region--Customer properties

        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        #endregion

        #region--COS properties

        public int CustomerOrderSlipId { get; set; }

        [ForeignKey(nameof(CustomerOrderSlipId))]
        public FilprideCustomerOrderSlip? CustomerOrderSlip { get; set; }

        #endregion

        [Column(TypeName = "varchar(200)")]
        public string Remarks { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal TotalAmount { get; set; }

        public bool IsPrinted { get; set; }

        public string Company { get; set; } = string.Empty;

        [Column(TypeName = "varchar(50)")]
        public string ManualDrNo { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal Demuragge { get; set; }

        public string Status { get; set; } = nameof(Utility.Status.Pending);

        #region Appointing Hauler

        public int HaulerId { get; set; }

        [ForeignKey(nameof(HaulerId))]
        public FilprideSupplier? Hauler { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Driver { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string PlateNo { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal Freight { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal ECC { get; set; }

        #endregion

        #region Booking of ATL

        [Column(TypeName = "varchar(20)")]
        public string? AuthorityToLoadNo { get; set; }

        #endregion

        public bool HasAlreadyInvoiced { get; set; }
    }
}