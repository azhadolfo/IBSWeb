using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;

namespace IBSWeb.Api
{
    public static class JournalVoucherApi
    {
        public static void MapJournalVoucherEndpoints(this WebApplication app)
        {
            app.MapPost("/api/journal-vouchers", async (CreateJournalVoucherDto dto, ApplicationDbContext db, IUnitOfWork uow, ILogger<Program> logger) =>
            {
                if (dto.Details.Count == 0)
                {
                    return Results.BadRequest(new { error = "At least one detail line is required." });
                }

                var totalDebit = Math.Round(dto.Details.Sum(d => d.Debit), 2);
                var totalCredit = Math.Round(dto.Details.Sum(d => d.Credit), 2);
                if (totalDebit != totalCredit)
                {
                    return Results.BadRequest(new { error = $"Debit ({totalDebit}) and Credit ({totalCredit}) must be equal." });
                }

                await using var tx = await db.Database.BeginTransactionAsync();

                try
                {
                    var jvNo = await uow.FilprideJournalVoucher.GenerateCodeAsync(dto.Company, dto.Type);

                    var header = new FilprideJournalVoucherHeader
                    {
                        Type = dto.Type,
                        JournalVoucherHeaderNo = jvNo,
                        Date = dto.Date,
                        References = dto.References,
                        Particulars = dto.Particulars,
                        CRNo = dto.CRNo,
                        JVReason = dto.JVReason,
                        CreatedBy = dto.CreatedBy,
                        Company = dto.Company,
                        JvType = dto.JvType,
                        Status = nameof(JvStatus.ForApproval)
                    };

                    db.Add(header);
                    await db.SaveChangesAsync();

                    var details = new List<FilprideJournalVoucherDetail>();
                    foreach (var detailDto in dto.Details)
                    {
                        var coa = await uow.FilprideChartOfAccount
                            .GetAsync(coa => coa.AccountNumber == detailDto.AccountNo);

                        details.Add(new FilprideJournalVoucherDetail
                        {
                            AccountNo = detailDto.AccountNo,
                            AccountName = coa?.AccountName ?? detailDto.AccountNo,
                            TransactionNo = jvNo,
                            JournalVoucherHeaderId = header.JournalVoucherHeaderId,
                            Debit = detailDto.Debit,
                            Credit = detailDto.Credit,
                        });
                    }

                    db.AddRange(details);
                    db.Add(new FilprideAuditTrail(dto.CreatedBy, $"Created new journal voucher# {jvNo}", "Journal Voucher", dto.Company));
                    await db.SaveChangesAsync();
                    await tx.CommitAsync();

                    return Results.Ok(new { journalVoucherHeaderNo = jvNo, journalVoucherHeaderId = header.JournalVoucherHeaderId });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create journal voucher via API");
                    await tx.RollbackAsync();
                    return Results.Problem(detail: ex.Message, statusCode: 400);
                }
            }).AllowAnonymous();
        }
    }

    public class CreateJournalVoucherDto
    {
        public DateOnly Date { get; set; }
        public string Particulars { get; set; } = null!;
        public string JVReason { get; set; } = null!;
        public string Company { get; set; } = null!;
        public string CreatedBy { get; set; } = null!;
        public string? References { get; set; }
        public string? CRNo { get; set; }
        public string Type { get; set; } = "Undocumented";
        public string JvType { get; set; } = "Reclass";
        public List<JournalVoucherDetailDto> Details { get; set; } = [];
    }

    public class JournalVoucherDetailDto
    {
        public string AccountNo { get; set; } = null!;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
}
