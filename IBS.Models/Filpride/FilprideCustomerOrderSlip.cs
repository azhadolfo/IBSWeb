﻿using IBS.Models.MasterFile;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride
{
    public class FilprideCustomerOrderSlip
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerOrderSlipId { get; set; }

        [Display(Name = "COS No.")]
        [Column(TypeName = "varchar(13)")]
        public string CustomerOrderSlipNo { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        #region--Customer properties

        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        #endregion

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Remarks { get; set; }

        [Column(TypeName = "varchar(100)")]
        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime DeliveryDateAndTime { get; set; }

        #region--Product properties

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        #endregion

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Vat { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal DeliveredPrice { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal DeliveredQuantity { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal BalanceQuantity { get; set; }

        public bool IsDelivered { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Column(TypeName = "varchar(100)")]
        public string? EditedBy { get; set; }

        public DateTime? EditedDate { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? DisapprovedBy { get; set; }

        public DateTime? DisapprovedDate { get; set; }

        public bool IsPrinted { get; set; }

        [Column(TypeName = "date")]
        public DateOnly? ExpirationDate { get; set; }
    }
}