using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class SalesInvoiceController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        public SalesInvoiceController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var salesInvoices = await _unitOfWork.FilprideSalesInvoice
                .GetAllAsync(null, cancellationToken);

            return View(salesInvoices);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            FilprideSalesInvoice viewModel = new()
            {
                Customers = await _unitOfWork.GetProductListAsyncById(cancellationToken),
                Products = await _unitOfWork.GetProductListAsyncById(cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(FilprideSalesInvoice model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    #region Saving Default Entries

                    var existingCustomer = await _unitOfWork.FilprideCustomer
                        .GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);

                    model.SalesInvoiceNo = await _unitOfWork.FilprideSalesInvoice.GenerateCodeAsync(cancellationToken);
                    model.CreatedBy = _userManager.GetUserName(User);
                    model.Amount = model.Quantity * model.UnitPrice;
                    model.DueDate = await _unitOfWork.FilprideSalesInvoice.ComputeDueDateAsync(existingCustomer.CustomerTerms, model.TransactionDate);

                    if (model.Amount >= model.Discount)
                    {
                        if (existingCustomer.CustomerType == "Vatable")
                        {
                            model.NetDiscount = model.Amount - model.Discount;
                            model.VatableSales = model.NetDiscount / 1.12m;
                            model.VatAmount = model.NetDiscount - model.VatableSales;
                            if (existingCustomer.WithHoldingTax)
                            {
                                model.WithHoldingTaxAmount = model.VatableSales * 0.01m;
                            }
                            if (existingCustomer.WithHoldingVat)
                            {
                                model.WithHoldingVatAmount = model.VatableSales * 0.05m;
                            }
                        }
                        else if (existingCustomer.CustomerType == "Zero Rated")
                        {
                            model.NetDiscount = model.Amount - model.Discount;
                            model.ZeroRated = model.Amount;

                            if (existingCustomer.WithHoldingTax)
                            {
                                model.WithHoldingTaxAmount = model.ZeroRated * 0.01m;
                            }
                            if (existingCustomer.WithHoldingVat)
                            {
                                model.WithHoldingVatAmount = model.ZeroRated * 0.05m;
                            }
                        }
                        else
                        {
                            model.NetDiscount = model.Amount - model.Discount;
                            model.VatExempt = model.Amount;
                            if (existingCustomer.WithHoldingTax)
                            {
                                model.WithHoldingTaxAmount = model.VatExempt * 0.01m;
                            }
                            if (existingCustomer.WithHoldingVat)
                            {
                                model.WithHoldingVatAmount = model.VatExempt * 0.05m;
                            }
                        }

                        await _unitOfWork.SaveAsync(cancellationToken);
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken);
                        model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                        TempData["error"] = "Please input below or exact amount based on the Sales Invoice";
                        return View(model);
                    }

                    #endregion Saving Default Entries
                }
                catch (Exception ex)
                {
                    model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken);
                    model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }

            model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken);
            model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(model);
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomerDetails(int customerId, CancellationToken cancellationToken)
        {
            var customer = await _unitOfWork.FilprideCustomer.GetAsync(c => c.CustomerId == customerId, cancellationToken);
            if (customer != null)
            {
                return Json(new
                {
                    SoldTo = customer.CustomerName,
                    customer.CustomerAddress,
                    customer.CustomerTin,
                    customer.BusinessStyle,
                    customer.CustomerTerms,
                    customer.CustomerType,
                    customer.WithHoldingTax
                });
            }
            return Json(null); // Return null if no matching customer is found
        }

        [HttpGet]
        public async Task<JsonResult> GetProductDetails(int productId, CancellationToken cancellationToken)
        {
            var product = await _unitOfWork.Product.GetAsync(c => c.ProductId == productId, cancellationToken);
            if (product != null)
            {
                return Json(new
                {
                    product.ProductName,
                    product.ProductUnit
                });
            }
            return Json(null); // Return null if no matching product is found
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            try
            {
                var salesInvoice = await _unitOfWork.FilprideSalesInvoice.GetAsync(si => si.SalesInvoiceId == id, cancellationToken);
                salesInvoice.Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken);
                salesInvoice.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                return View(salesInvoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred. Please try again later.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(FilprideSalesInvoice model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingRecord = await _unitOfWork.FilprideSalesInvoice.GetAsync(si => si.SalesInvoiceId == model.SalesInvoiceId, cancellationToken);

                    if (existingRecord == null)
                    {
                        return NotFound();
                    }

                    if (existingRecord.Amount >= model.Discount)
                    {
                        if (existingRecord.Customer.CustomerType == "Vatable")
                        {
                            existingRecord.NetDiscount = existingRecord.Amount - model.Discount;
                            existingRecord.VatableSales = existingRecord.NetDiscount / 1.12m;
                            existingRecord.VatAmount = existingRecord.NetDiscount - existingRecord.VatableSales;
                            if (existingRecord.Customer.WithHoldingTax)
                            {
                                existingRecord.WithHoldingTaxAmount = existingRecord.VatableSales * (decimal)0.01;
                            }
                            if (existingRecord.Customer.WithHoldingVat)
                            {
                                existingRecord.WithHoldingVatAmount = existingRecord.VatableSales * (decimal)0.05;
                            }
                        }
                        else if (existingRecord.Customer.CustomerType == "Zero Rated")
                        {
                            existingRecord.NetDiscount = existingRecord.Amount - model.Discount;
                            existingRecord.ZeroRated = existingRecord.Amount;

                            if (existingRecord.Customer.WithHoldingTax)
                            {
                                existingRecord.WithHoldingTaxAmount = existingRecord.ZeroRated * 0.01m;
                            }
                            if (existingRecord.Customer.WithHoldingVat)
                            {
                                existingRecord.WithHoldingVatAmount = existingRecord.ZeroRated * 0.05m;
                            }
                        }
                        else
                        {
                            existingRecord.NetDiscount = existingRecord.Amount - model.Discount;
                            existingRecord.VatExempt = existingRecord.Amount;
                            if (existingRecord.Customer.WithHoldingTax)
                            {
                                existingRecord.WithHoldingTaxAmount = existingRecord.VatExempt * 0.01m;
                            }
                            if (existingRecord.Customer.WithHoldingVat)
                            {
                                existingRecord.WithHoldingVatAmount = existingRecord.VatExempt * 0.05m;
                            }
                        }

                        await _unitOfWork.SaveAsync(cancellationToken);
                        TempData["success"] = "Sales invoice updated successfully";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken);
                    model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }

            model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken);
            model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(model);
        }

        public async Task<IActionResult> PrintInvoice(int id, CancellationToken cancellationToken)
        {
            var sales = await _unitOfWork.FilprideSalesInvoice.GetAsync(si => si.SalesInvoiceId == id, cancellationToken);
            return View(sales);
        }
    }
}