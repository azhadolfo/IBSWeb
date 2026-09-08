using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Api
{
    public static class JournalVoucherApi
    {
        /// <summary>
        /// Registers the journal voucher API endpoints, including the POST endpoint for creating
        /// a balanced journal voucher with header and detail lines in a single transaction.
        /// </summary>
        public static void MapJournalVoucherEndpoints(this WebApplication app, string dmcmApiKey)
        {
            app.MapPost("/api/journal-vouchers", async (CreateJournalVoucherDto dto, ApplicationDbContext db,
                IUnitOfWork uow, ILogger<Program> logger) =>
            {
                if (dto.Details is not { Count: > 0 })
                {
                    return Results.BadRequest(new { error = "At least one detail line is required." });
                }
                if (string.IsNullOrWhiteSpace(dto.Company) ||
                    string.IsNullOrWhiteSpace(dto.CreatedBy) ||
                    string.IsNullOrWhiteSpace(dto.Particulars) ||
                    string.IsNullOrWhiteSpace(dto.JVReason))
                {
                    return Results.BadRequest(new
                    {
                        error = "Company, CreatedBy, Particulars, and JVReason are required."
                    });
                }

                if (dto.Date == DateOnly.MinValue)
                {
                    return Results.BadRequest(new { error = "Date is required." });
                }

                if (dto.Details.Any(detail => detail is null))
                {
                    return Results.BadRequest(new { error = "Detail lines cannot be null." });
                }

                // Round once, canonically, and reuse everywhere — validation and persistence must agree.
                var roundedDetails = dto.Details.Select(d => new
                {
                    d.AccountNo,
                    Debit = Math.Round(d.Debit, 2),
                    Credit = Math.Round(d.Credit, 2)
                }).ToList();

                if (roundedDetails.Any(d => d.Debit < 0 || d.Credit < 0))
                {
                    return Results.BadRequest(new { error = "Debit and Credit amounts cannot be negative." });
                }

                var totalDebit = roundedDetails.Sum(d => d.Debit);
                var totalCredit = roundedDetails.Sum(d => d.Credit);
                if (totalDebit != totalCredit || totalDebit == 0)
                {
                    return Results.BadRequest(new { error = "Debit and Credit totals must be equal and greater than zero." });
                }

                if (dto.Type is not (nameof(DocumentType.Documented) or nameof(DocumentType.Undocumented)))
                {
                    return Results.BadRequest(new { error = $"Type must be '{nameof(DocumentType.Documented)}' or '{nameof(DocumentType.Undocumented)}'." });
                }

                await using var tx = await db.Database.BeginTransactionAsync();

                try
                {
                    string jvNo;
                    FilprideJournalVoucherHeader header;
                    DbUpdateException? conflictException = null;

                    // Retry on the JV-no unique constraint when concurrent requests race to grab the same number.
                    for (var attempt = 1; ; attempt++)
                    {
                        jvNo = await uow.FilprideJournalVoucher.GenerateCodeAsync(dto.Company, dto.Type);

                        header = new FilprideJournalVoucherHeader
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

                        try
                        {
                            db.Add(header);
                            await db.SaveChangesAsync();
                            break;
                        }
                        catch (DbUpdateException ex) when (attempt < 3 && IsUniqueViolation(ex))
                        {
                            db.ChangeTracker.Clear();
                        }
                        catch (DbUpdateException ex) when (attempt >= 3 && IsUniqueViolation(ex))
                        {
                            db.ChangeTracker.Clear();
                            conflictException = ex;
                            break;
                        }
                    }

                    if (conflictException is not null)
                    {
                        await tx.RollbackAsync();
                        return Results.Conflict(new { error = "Journal voucher number conflict after retries. Please retry." });
                    }

                    var details = new List<FilprideJournalVoucherDetail>();
                    for (var i = 0; i < dto.Details.Count; i++)
                    {
                        var detailDto = dto.Details[i];
                        var rounded = roundedDetails[i];

                        var coa = await uow.FilprideChartOfAccount
                            .GetAsync(coa => coa.AccountNumber == detailDto.AccountNo);

                        if (coa == null)
                        {
                            await tx.RollbackAsync();
                            return Results.BadRequest(new { error = $"Unknown account number: {detailDto.AccountNo}" });
                        }

                        details.Add(new FilprideJournalVoucherDetail
                        {
                            AccountNo = detailDto.AccountNo,
                            AccountName = coa.AccountName,
                            TransactionNo = jvNo,
                            JournalVoucherHeaderId = header.JournalVoucherHeaderId,
                            Debit = rounded.Debit,
                            Credit = rounded.Credit,
                        });
                    }

                    db.AddRange(details);
                    db.Add(new FilprideAuditTrail(dto.CreatedBy, $"Created new journal voucher# {jvNo}",
                        "Journal Voucher", dto.Company));
                    await db.SaveChangesAsync();
                    await tx.CommitAsync();

                    return Results.Ok(new
                    {
                        journalVoucherHeaderNo = jvNo, journalVoucherHeaderId = header.JournalVoucherHeaderId
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create journal voucher via API");
                    await tx.RollbackAsync();
                    return Results.Problem(statusCode: StatusCodes.Status500InternalServerError);
                }
            }).AddEndpointFilter(async (context, next) =>
            {
                var apiKey = context.HttpContext.Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey) || apiKey != dmcmApiKey)
                {
                    return Results.Unauthorized();
                }

                return await next(context);
            })
            .AllowAnonymous(); // no ASP.NET user principal; access is gated by the X-API-Key filter above
        }

        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is Npgsql.PostgresException { SqlState: "23505" };
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
