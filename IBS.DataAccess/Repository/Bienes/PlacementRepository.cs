using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Bienes.IRepository;
using IBS.Models.Bienes;
using IBS.Models.Bienes.ViewModels;
using IBS.Models.Filpride.Books;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Bienes
{
    public class PlacementRepository : Repository<BienesPlacement>, IPlacementRepository
    {
        private ApplicationDbContext _db;

        public PlacementRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateControlNumberAsync(int companyId, CancellationToken cancellationToken = default)
        {
            var company = await _db.Companies.FindAsync(companyId, cancellationToken);

            var lastRecord = await _db.BienesPlacements
                .Where(p => p.CompanyId == companyId)
                .OrderByDescending(p => p.ControlNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastRecord == null)
            {
                return $"{company.CompanyName.ToUpper()}-000001";
            }

            string lastSeries = lastRecord.ControlNumber;
            string numericPart = lastSeries.Substring(company.CompanyName.Length + 1);
            int incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, company.CompanyName.Length) + "-" + incrementedNumber.ToString("D6");
        }

        public async Task UpdateAsync(PlacementViewModel viewModel, CancellationToken cancellationToken = default)
        {
            var existingRecord = await _db.BienesPlacements
                .FirstOrDefaultAsync(p => p.PlacementId == viewModel.PlacementId, cancellationToken);

            existingRecord!.CompanyId = viewModel.CompanyId;
            existingRecord.BankId = viewModel.BankId;
            existingRecord.Bank = viewModel.Bank;
            existingRecord.Branch = viewModel.Branch;
            existingRecord.TDAccountNumber = viewModel.TDAccountNumber;
            existingRecord.AccountName = viewModel.AccountName;
            existingRecord.SettlementAccountId = viewModel.SettlementAccountId;
            existingRecord.DateFrom = viewModel.FromDate;
            existingRecord.DateTo = viewModel.ToDate;
            existingRecord.Remarks = viewModel.Remarks;
            existingRecord.ChequeNumber = viewModel.ChequeNumber;
            existingRecord.CVNo = viewModel.CVNo;
            existingRecord.BatchNumber = viewModel.BatchNumber;
            existingRecord.PrincipalAmount = viewModel.PrincipalAmount;
            existingRecord.PrincipalDisposition = viewModel.PrincipalDisposition;
            existingRecord.PlacementType = viewModel.PlacementType;
            existingRecord.InterestRate = viewModel.InterestRate / 100;
            existingRecord.HasEWT = viewModel.HasEwt;
            existingRecord.EWTRate = viewModel.EWTRate / 100;
            existingRecord.HasTrustFee = viewModel.HasTrustFee;
            existingRecord.TrustFeeRate = viewModel.TrustFeeRate / 100;
            existingRecord.LockedDate = viewModel.ToDate.AddDays(2).ToDateTime(TimeOnly.MinValue);

            if (existingRecord.PlacementType == PlacementType.LongTerm)
            {
               existingRecord.NumberOfYears = viewModel.NumberOfYears;
               existingRecord.FrequencyOfPayment = viewModel.FrequencyOfPayment;
            }

            if (_db.ChangeTracker.HasChanges())
            {
                existingRecord.EditedBy = viewModel.CurrentUser;
                existingRecord.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                FilprideAuditTrail auditTrailBook = new(existingRecord.EditedBy, $"Edit placement# {existingRecord.ControlNumber}", "Placement", nameof(Bienes));
                await _db.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }

        }

        public async Task RollOverAsync(BienesPlacement model, string user, CancellationToken cancellationToken = default)
        {
            BienesPlacement newPlacement = new()
            {
                ControlNumber = model.ControlNumber,
                CompanyId = model.CompanyId,
                BankId = model.BankId,
                Bank = model.Bank,
                Branch = model.Branch,
                TDAccountNumber = model.TDAccountNumber,
                AccountName = model.AccountName,
                SettlementAccountId = model.SettlementAccountId,
                DateFrom = default,
                DateTo = default,
                Remarks = model.Remarks,
                ChequeNumber = model.ChequeNumber,
                CVNo = model.CVNo,
                BatchNumber = model.BatchNumber,
                PrincipalAmount = model.PrincipalAmount,
                PrincipalDisposition = model.PrincipalDisposition,
                PlacementType = model.PlacementType,
                InterestRate = model.InterestRate,
                HasEWT = model.HasEWT,
                EWTRate = model.EWTRate,
                HasTrustFee = model.HasTrustFee,
                TrustFeeRate = model.TrustFeeRate,
                CreatedBy = user,
                NumberOfYears = model.NumberOfYears,
                FrequencyOfPayment = model.FrequencyOfPayment,
                RolledFromId = model.PlacementId,
            };

            await _db.BienesPlacements.AddAsync(newPlacement, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<string> SwappingAsync(BienesPlacement model, int companyId, string user, CancellationToken cancellationToken = default)
        {

            BienesPlacement newPlacement = new()
            {
                ControlNumber = await GenerateControlNumberAsync(companyId, cancellationToken),
                CompanyId = model.CompanyId,
                BankId = model.BankId,
                Bank = model.Bank,
                Branch = model.Branch,
                TDAccountNumber = model.TDAccountNumber,
                AccountName = model.AccountName,
                SettlementAccountId = model.SettlementAccountId,
                DateFrom = default,
                DateTo = default,
                Remarks = model.Remarks,
                ChequeNumber = model.ChequeNumber,
                CVNo = model.CVNo,
                BatchNumber = model.BatchNumber,
                PrincipalAmount = model.PrincipalAmount,
                PrincipalDisposition = model.PrincipalDisposition,
                PlacementType = model.PlacementType,
                InterestRate = model.InterestRate,
                HasEWT = model.HasEWT,
                EWTRate = model.EWTRate,
                HasTrustFee = model.HasTrustFee,
                TrustFeeRate = model.TrustFeeRate,
                CreatedBy = user,
                NumberOfYears = model.NumberOfYears,
                FrequencyOfPayment = model.FrequencyOfPayment,
                SwappedFromId = model.PlacementId,
            };

            await _db.BienesPlacements.AddAsync(newPlacement, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return newPlacement.ControlNumber;
        }

        public override async Task<BienesPlacement> GetAsync(Expression<Func<BienesPlacement, bool>> filter,
            CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(p => p.Company)
                .Include(p => p.BankAccount)
                .Include(p => p.SettlementAccount)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<BienesPlacement>> GetAllAsync(Expression<Func<BienesPlacement, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<BienesPlacement> query = dbSet
                .Include(p => p.Company)
                .Include(p => p.BankAccount);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
