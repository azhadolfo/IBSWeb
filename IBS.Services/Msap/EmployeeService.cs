using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;
using System.Linq.Dynamic.Core;

namespace IBS.Services.Msap
{
    public class EmployeeService(
        IUnitOfWork unitOfWork)
        : IEmployeeService
    {
        public async Task<IEnumerable<Employee>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await unitOfWork.Employee.GetAllAsync(e => e.IsActive, cancellationToken);
        }

        public async Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.Employee.GetAsync(e => e.EmployeeId == id, cancellationToken);
        }

        public async Task<ServiceResult<int>> CreateAsync(Employee model, string? companyClaims, string username, CancellationToken cancellationToken)
        {
            try
            {
                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    model.Company = companyClaims;
                    await unitOfWork.Employee.AddAsync(model, cancellationToken);

                    AuditTrail auditTrail = new(username, $"Created new Employee #{model.EmployeeNumber}", "Employee");
                    await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                    await unitOfWork.SaveAsync(cancellationToken);
                }, cancellationToken);

                return ServiceResult<int>.Success(model.EmployeeId, $"Employee {model.EmployeeNumber} created successfully");
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure($"Failed to create employee: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> UpdateAsync(Employee model, string username, CancellationToken cancellationToken)
        {
            try
            {
                var existingModel = await GetByIdAsync(model.EmployeeId, cancellationToken);

                if (existingModel == null)
                {
                    return ServiceResult.Failure("Employee not found.", ServiceResultStatus.NotFound);
                }

            var changes = new List<string>();
            if (existingModel.EmployeeNumber != model.EmployeeNumber)
            {
                changes.Add($"Employee Number: {existingModel.EmployeeNumber} → {model.EmployeeNumber}");
            }

            if (existingModel.FirstName != model.FirstName)
            {
                changes.Add($"First Name: {existingModel.FirstName} → {model.FirstName}");
            }

            if (existingModel.LastName != model.LastName)
            {
                changes.Add($"Last Name: {existingModel.LastName} → {model.LastName}");
            }

            if (existingModel.Department != model.Department)
            {
                changes.Add($"Department: {existingModel.Department} → {model.Department}");
            }

            if (existingModel.Position != model.Position)
            {
                changes.Add($"Position: {existingModel.Position} → {model.Position}");
            }

            if (existingModel.IsActive != model.IsActive)
            {
                changes.Add($"Status: {(existingModel.IsActive ? "Active" : "Inactive")} → {(model.IsActive ? "Active" : "Inactive")}");
            }

            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                if (changes.Any())
                {
                    AuditTrail auditTrail = new(username, $"Edited Employee #{existingModel.EmployeeNumber}: {string.Join("; ", changes)}", "Employee");
                    await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);
                }

                existingModel.EmployeeNumber = model.EmployeeNumber;
                existingModel.Initial = model.Initial;
                existingModel.FirstName = model.FirstName;
                existingModel.MiddleName = model.MiddleName;
                existingModel.LastName = model.LastName;
                existingModel.Suffix = model.Suffix;
                existingModel.BirthDate = model.BirthDate;
                existingModel.TelNo = model.TelNo;
                existingModel.SssNo = model.SssNo;
                existingModel.TinNo = model.TinNo;
                existingModel.PhilhealthNo = model.PhilhealthNo;
                existingModel.PagibigNo = model.PagibigNo;
                existingModel.Department = model.Department;
                existingModel.DateHired = model.DateHired;
                existingModel.DateResigned = model.DateResigned;
                existingModel.Position = model.Position;
                existingModel.IsManagerial = model.IsManagerial;
                existingModel.Supervisor = model.Supervisor;
                existingModel.Salary = model.Salary;
                existingModel.IsActive = model.IsActive;
                existingModel.Status = model.Status;
                existingModel.Address = model.Address;

                await unitOfWork.SaveAsync(cancellationToken);
            }, cancellationToken);

            return ServiceResult.Success("Employee edited successfully");
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"Failed to edit employee: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<(IEnumerable<Employee> Data, int TotalRecords)> GetPagedEmployeesAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            System.Linq.Expressions.Expression<Func<Employee, bool>>? filter = null;
            if (!string.IsNullOrEmpty(parameters.Search.Value))
            {
                var searchValue = parameters.Search.Value.ToLower();
                filter = e =>
                    e.EmployeeNumber.ToLower().Contains(searchValue) ||
                    (e.Initial != null && e.Initial.ToLower().Contains(searchValue)) ||
                    e.FirstName.ToLower().Contains(searchValue) ||
                    e.LastName.ToLower().Contains(searchValue) ||
                    (e.TelNo != null && e.TelNo.ToLower().Contains(searchValue)) ||
                    (e.Department != null && e.Department.ToLower().Contains(searchValue)) ||
                    e.Position.ToLower().Contains(searchValue);
            }

            var queried = await unitOfWork.Employee.GetAllAsync(filter, cancellationToken);

            if (parameters.Order?.Count > 0)
            {
                var orderColumn = parameters.Order[0];
                var columnName = parameters.Columns[orderColumn.Column].Data;
                var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                queried = queried
                    .AsQueryable()
                    .OrderBy($"{columnName} {sortDirection}")
                    .ToList();
            }

            var totalRecords = queried.Count();
            var pagedData = queried
                .Skip(parameters.Start)
                .Take(parameters.Length)
                .ToList();

            return (pagedData, totalRecords);
        }
    }
}
