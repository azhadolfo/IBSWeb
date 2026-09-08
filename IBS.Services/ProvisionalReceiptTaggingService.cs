using System.ComponentModel.DataAnnotations;
using IBS.DataAccess.Data;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.Services
{
    public class ProvisionalReceiptTaggingService
    {
        private readonly ApplicationDbContext _db;

        public ProvisionalReceiptTaggingService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<FilprideCollectionCategory>> GetCategoriesAsync(int? retainedId, CancellationToken ct)
        {
            return await _db.FilprideCollectionCategories.AsNoTracking()
                .Where(c => c.IsActive || c.Id == retainedId)
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        }

        public static int? GetTagId(FilprideProvisionalReceipt receipt)
        {
            return receipt.TagType switch
            {
                CollectionTagType.Company => receipt.TaggedCompanyId,
                CollectionTagType.Employee => receipt.TaggedSupplierId,
                CollectionTagType.BankAccount => receipt.TaggedBankAccountId,
                _ => null
            };
        }

        public async Task<List<SelectListItem>> GetOptionsAsync(CollectionTagType? type, int? retainedId, CancellationToken ct)
        {
            return type switch
            {
                CollectionTagType.Company => await _db.Companies.AsNoTracking()
                    .Where(c => c.IsActive || c.CompanyId == retainedId)
                    .OrderBy(c => c.CompanyName)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CompanyId.ToString(),
                        Text = c.CompanyCode + " - " + c.CompanyName
                    })
                    .ToListAsync(ct),
                CollectionTagType.Employee => await _db.FilprideSuppliers.AsNoTracking()
                    .Where(s => (s.IsActive && s.Category == "Employee") || s.SupplierId == retainedId)
                    .OrderBy(s => s.SupplierName)
                    .Select(s => new SelectListItem
                    {
                        Value = s.SupplierId.ToString(),
                        Text = s.EmployeeNumber + " - " + s.SupplierName
                    })
                    .ToListAsync(ct),
                CollectionTagType.BankAccount => await _db.FilprideBankAccounts.AsNoTracking()
                    .Where(b => b.IsActive || b.BankAccountId == retainedId)
                    .OrderBy(b => b.AccountName)
                    .Select(b => new SelectListItem
                    {
                        Value = b.BankAccountId.ToString(),
                        Text = b.Bank + " - " + b.AccountNo + " - " + b.AccountName
                    })
                    .ToListAsync(ct),
                _ => []
            };
        }

        public async Task<string?> ValidateAsync(ProvisionalReceiptViewModel form, FilprideProvisionalReceipt? existing, CancellationToken ct)
        {
            var category = await _db.FilprideCollectionCategories
                .SingleOrDefaultAsync(c => c.Id == form.CollectionCategoryId, ct);
            if (category == null || (!category.IsActive && existing?.CollectionCategoryId != category.Id))
            {
                return "Select an active collection category. If none are available, ask an administrator to set up collection categories.";
            }
            if (form.TagType == null)
            {
                if (category.TaggingRequirement == CollectionTaggingRequirement.Required)
                {
                    return "This category requires a master-file tag.";
                }
                if (form.TagId != null)
                {
                    return "A master-file record must have a matching type.";
                }
            }
            else
            {
                if (category.TaggingRequirement == CollectionTaggingRequirement.None || !category.Allows(form.TagType.Value))
                {
                    return "The selected master-file type is not allowed for this category.";
                }
                if (form.TagId is null or <= 0)
                {
                    return "Select a master-file record.";
                }
                var retain = existing != null && existing.CollectionCategoryId == form.CollectionCategoryId &&
                             existing.TagType == form.TagType && GetTagId(existing) == form.TagId;
                var tagId = form.TagId.Value;
                switch (form.TagType.Value)
                {
                    case CollectionTagType.Company:
                        var company = await _db.Companies.AsNoTracking()
                            .SingleOrDefaultAsync(c => c.CompanyId == tagId && (c.IsActive || retain), ct);
                        if (company == null)
                        {
                            return "Select an eligible active master-file record.";
                        }
                        form.PayerName = retain ? existing!.PayerName : company.CompanyName;
                        form.PayerAddress = retain ? existing!.PayerAddress : company.CompanyAddress;
                        break;

                    case CollectionTagType.Employee:
                        var employee = await _db.FilprideSuppliers.AsNoTracking()
                            .SingleOrDefaultAsync(s => s.SupplierId == tagId &&
                                ((s.IsActive && s.Category == "Employee") || retain), ct);
                        if (employee == null)
                        {
                            return "Select an eligible active master-file record.";
                        }
                        form.PayerName = retain ? existing!.PayerName : employee.SupplierName;
                        form.PayerAddress = retain ? existing!.PayerAddress : employee.SupplierAddress;
                        break;

                    case CollectionTagType.BankAccount:
                        var bankAccount = await _db.FilprideBankAccounts.AsNoTracking()
                            .SingleOrDefaultAsync(b => b.BankAccountId == tagId && (b.IsActive || retain), ct);
                        if (bankAccount == null)
                        {
                            return "Select an eligible active master-file record.";
                        }
                        break;
                }
            }
            form.PayerName = form.PayerName?.Trim();
            form.PayerAddress = form.PayerAddress?.Trim();
            if (string.IsNullOrWhiteSpace(form.PayerName))
            {
                return "Received From is required.";
            }
            if (form.PayerName.Length > 200 || form.PayerAddress?.Length > 500)
            {
                return "Payer name must be at most 200 characters and address at most 500 characters.";
            }
            return null;
        }

        public async Task SaveCategoryAsync(CollectionCategoryViewModel form, string user, CancellationToken ct)
        {
            if (!Enum.IsDefined(form.TaggingRequirement))
            {
                throw new ValidationException("Select a valid tagging requirement.");
            }
            form.Name = form.Name.Trim();
            if (form.TaggingRequirement == CollectionTaggingRequirement.None)
            {
                form.AllowCompany = form.AllowEmployee = form.AllowBankAccount = false;
            }
            else if (!form.AllowCompany && !form.AllowEmployee && !form.AllowBankAccount)
            {
                throw new ValidationException("Select at least one allowed master-file type.");
            }
            await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            FilprideCollectionCategory category;
            if (form.Id == 0)
            {
                category = new FilprideCollectionCategory();
            }
            else
            {
                category = await _db.FilprideCollectionCategories
                    .SingleOrDefaultAsync(c => c.Id == form.Id, ct)
                    ?? throw new ValidationException("Collection category not found.");
            }
            var used = await _db.FilprideProvisionalReceipts.AnyAsync(p => p.CollectionCategoryId == category.Id, ct);
            if (used && (category.Name != form.Name || category.TaggingRequirement != form.TaggingRequirement ||
                         category.AllowCompany != form.AllowCompany || category.AllowEmployee != form.AllowEmployee || category.AllowBankAccount != form.AllowBankAccount))
            {
                throw new ValidationException("This category is already used. Only its active status can be changed.");
            }
            if (await _db.FilprideCollectionCategories.AnyAsync(c => c.Id != form.Id && c.Name.ToLower() == form.Name.ToLower(), ct))
            {
                throw new ValidationException("A collection category with this name already exists.");
            }
            category.Name = form.Name;
            category.TaggingRequirement = form.TaggingRequirement;
            category.AllowCompany = form.AllowCompany;
            category.AllowEmployee = form.AllowEmployee;
            category.AllowBankAccount = form.AllowBankAccount;
            category.IsActive = form.IsActive;
            var now = DateTimeHelper.GetCurrentPhilippineTime();
            if (form.Id == 0)
            {
                category.CreatedBy = user;
                category.CreatedDate = now;
                _db.FilprideCollectionCategories.Add(category);
            }
            else
            {
                category.EditedBy = user;
                category.EditedDate = now;
            }
            _db.FilprideAuditTrails.Add(new FilprideAuditTrail(user,
                $"{(form.Id == 0 ? "Created" : "Updated")} collection category {category.Name}; tagging {category.TaggingRequirement}; " +
                $"company {category.AllowCompany}, employee {category.AllowEmployee}, bank {category.AllowBankAccount}; active {category.IsActive}", "Collection Category"));
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
    }
}
