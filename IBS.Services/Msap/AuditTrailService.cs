using IBS.DataAccess.Repository.IRepository;
using IBS.Models;

namespace IBS.Services.Msap
{
    public class AuditTrailService(IUnitOfWork unitOfWork) : IAuditTrailService
    {
        public async Task<IEnumerable<AuditTrail>> GetAuditTrailsByEntityAsync(string documentType, int recordId, CancellationToken cancellationToken)
        {
            return await unitOfWork.AuditTrail.GetAllAsync(
                a => a.DocumentType == documentType && a.RecordId == recordId,
                cancellationToken);
        }

        public async Task<IEnumerable<AuditTrail>> GetJobOrderTimelineAsync(int jobOrderId, CancellationToken cancellationToken)
        {
            var jobOrder = await unitOfWork.JobOrder.GetAsync(jo => jo.JobOrderId == jobOrderId, cancellationToken);
            if (jobOrder == null)
            {
                return Enumerable.Empty<AuditTrail>();
            }

            var joNumber = jobOrder.JobOrderNumber;

            var dispatchTickets = await unitOfWork.DispatchTicket.GetAllAsync(dt => dt.JobOrderId == jobOrderId, cancellationToken);
            var dtIds = dispatchTickets.Select(dt => dt.DispatchTicketId).ToList();
            var dtNumbers = dispatchTickets.Select(dt => dt.DispatchNumber).Where(n => !string.IsNullOrEmpty(n)).ToList();

            var billings = await unitOfWork.Billing.GetAllAsync(b => b.JobOrderId == jobOrderId, cancellationToken);
            var billingIds = billings.Select(b => b.MsapBillingId).ToList();
            var billingNumbers = billings.Select(b => b.MsapBillingNumber).Where(n => !string.IsNullOrEmpty(n)).ToList();

            var collectionIds = billings.Where(b => b.CollectionId.HasValue).Select(b => b.CollectionId!.Value).Distinct().ToList();
            var collectionNumbers = billings.Where(b => !string.IsNullOrEmpty(b.CollectionNumber)).Select(b => b.CollectionNumber!).Distinct().ToList();

            var allAudits = new List<AuditTrail>();

            allAudits.AddRange(await unitOfWork.AuditTrail.GetAllAsync(a =>
                a.DocumentType == "Job Order" && (a.RecordId == jobOrderId || a.ReferenceNumber == joNumber || a.Activity.Contains(joNumber)),
                cancellationToken));

            if (dtIds.Any() || dtNumbers.Any())
            {
                allAudits.AddRange(await unitOfWork.AuditTrail.GetAllAsync(a =>
                    (a.DocumentType == "Dispatch Ticket" || a.DocumentType == "Tariff") &&
                    ((a.RecordId.HasValue && dtIds.Contains(a.RecordId.Value)) || (a.ReferenceNumber != null && dtNumbers.Contains(a.ReferenceNumber))),
                    cancellationToken));
            }

            if (billingIds.Any() || billingNumbers.Any())
            {
                allAudits.AddRange(await unitOfWork.AuditTrail.GetAllAsync(a =>
                    a.DocumentType == "Billing" &&
                    ((a.RecordId.HasValue && billingIds.Contains(a.RecordId.Value)) || (a.ReferenceNumber != null && billingNumbers.Contains(a.ReferenceNumber))),
                    cancellationToken));
            }

            if (collectionIds.Any() || collectionNumbers.Any())
            {
                allAudits.AddRange(await unitOfWork.AuditTrail.GetAllAsync(a =>
                    a.DocumentType == "Collection" &&
                    ((a.RecordId.HasValue && collectionIds.Contains(a.RecordId.Value)) || (a.ReferenceNumber != null && collectionNumbers.Contains(a.ReferenceNumber))),
                    cancellationToken));
            }

            return allAudits
                .GroupBy(a => a.Id)
                .Select(g => g.First())
                .OrderByDescending(a => a.Date)
                .ToList();
        }

        public async Task<(IEnumerable<AuditTrail> Data, int RecordsFiltered, int TotalRecords)> GetPagedAuditTrailsAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            return await unitOfWork.AuditTrail.GetPagedAuditTrailsAsync(parameters, cancellationToken);
        }
    }
}
