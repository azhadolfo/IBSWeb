﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        private ApplicationDbContext _db;

        public CompanyRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync()
        {
            Company? lastCompany = await _db
                .Companies
                .OrderBy(c => c.CompanyId)
                .LastOrDefaultAsync();

            if (lastCompany != null)
            {
                // Extract the last company code
                string lastCode = lastCompany.CompanyCode;

                // Extract the numeric part of the code
                string numericPart = lastCode.Substring(1); // Assuming code format starts with a letter followed by numeric part

                // Parse the numeric part and increment it by one
                int incrementedNumber = int.Parse(numericPart) + 1;

                // Format the incremented number with leading zeros and concatenate with the letter part
                return $"{lastCode[0]}{incrementedNumber:D2}"; //e.g C02
            }
            else
            {
                return "C01";
            }
        }

        public async Task<bool> IsCompanyExistAsync(string companyName)
        {
            return await _db.Companies
                .AnyAsync(c => c.CompanyName == companyName);
        }

        public async Task<bool> IsTinNoExistAsync(string tinNo)
        {
            return await _db.Companies
                .AnyAsync(c => c.CompanyTin == tinNo);
        }

        public async Task UpdateAsync(Company model)
        {
            Company? existingCompany = await _db.Companies
                .FindAsync(model.CompanyId);

            existingCompany!.CompanyName = model.CompanyName;
            existingCompany!.CompanyAddress = model.CompanyAddress;
            existingCompany!.CompanyTin = model.CompanyTin;
            existingCompany!.BusinessStyle = model.BusinessStyle;
            existingCompany!.EditedBy = "Ako";
            existingCompany!.EditedDate = DateTime.Now;
        }
    }
}