using IBS.DataAccess.Data;
using IBS.DTOs;
using IBS.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace IBS.Services
{
    public interface ISubAccountResolver
    {
        Task<SubAccountInfoDto?> ResolveAsync(SubAccountType type,
            int subAccountId,
            CancellationToken cancellationToken = default);
        Task<bool> ValidateExistsAsync(SubAccountType type,
            int subAccountId,
            CancellationToken cancellationToken = default);
    }

    public class SubAccountResolver : ISubAccountResolver
    {
        private readonly ApplicationDbContext _context;

        public SubAccountResolver(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SubAccountInfoDto?> ResolveAsync(SubAccountType type,
            int subAccountId,
            CancellationToken cancellationToken = default)
        {
            switch (type)
            {
                case SubAccountType.Customer:
                    var customer = await _context.FilprideCustomers
                        .Where(c => c.CustomerId == subAccountId)
                        .Select(c => new SubAccountInfoDto
                        {
                            Type = SubAccountType.Customer,
                            Id = c.CustomerId,
                            Name = c.CustomerName
                        })
                        .FirstOrDefaultAsync(cancellationToken);
                    return customer;

                case SubAccountType.Supplier:
                    var supplier = await _context.FilprideSuppliers
                        .Where(s => s.SupplierId == subAccountId)
                        .Select(s => new SubAccountInfoDto
                        {
                            Type = SubAccountType.Supplier,
                            Id = s.SupplierId,
                            Name = s.SupplierName
                        })
                        .FirstOrDefaultAsync(cancellationToken);
                    return supplier;

                case SubAccountType.Employee:
                    var employee = await _context.FilprideEmployees
                        .Where(e => e.EmployeeId == subAccountId)
                        .Select(e => new SubAccountInfoDto
                        {
                            Type = SubAccountType.Employee,
                            Id = e.EmployeeId,
                            Name = $"{e.FirstName} {e.LastName}"
                        })
                        .FirstOrDefaultAsync(cancellationToken);
                    return employee;

                case SubAccountType.BankAccount:
                    var bank = await _context.FilprideBankAccounts
                        .Where(b => b.BankAccountId == subAccountId)
                        .Select(b => new SubAccountInfoDto
                        {
                            Type = SubAccountType.BankAccount,
                            Id = b.BankAccountId,
                            Name = $"{b.AccountNo} {b.AccountName}"
                        })
                        .FirstOrDefaultAsync(cancellationToken);
                    return bank;

                case SubAccountType.Company:
                    var company = await _context.Companies
                        .Where(c => c.CompanyId == subAccountId)
                        .Select(c => new SubAccountInfoDto
                        {
                            Type = SubAccountType.Company,
                            Id = c.CompanyId,
                            Name = c.CompanyName
                        })
                        .FirstOrDefaultAsync(cancellationToken);
                    return company;

                default:
                    return null;
            }
        }

        public async Task<bool> ValidateExistsAsync(SubAccountType type,
            int subAccountId,
            CancellationToken cancellationToken = default)
        {
            return type switch
            {
                SubAccountType.Customer => await _context.FilprideCustomers
                    .AnyAsync(c => c.CustomerId == subAccountId, cancellationToken),
                SubAccountType.Supplier => await _context.FilprideSuppliers
                    .AnyAsync(s => s.SupplierId == subAccountId, cancellationToken),
                SubAccountType.Employee => await _context.FilprideEmployees
                    .AnyAsync(e => e.EmployeeId == subAccountId, cancellationToken),
                SubAccountType.BankAccount => await _context.FilprideBankAccounts
                    .AnyAsync(b => b.BankAccountId == subAccountId, cancellationToken),
                SubAccountType.Company => await _context.Companies
                    .AnyAsync(c => c.CompanyId == subAccountId, cancellationToken),
                _ => false
            };
        }
    }
}
