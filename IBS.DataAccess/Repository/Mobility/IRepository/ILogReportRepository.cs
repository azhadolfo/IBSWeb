namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface ILogReportRepository
    {
        public Task LogChangesAsync(int id, Dictionary<string, (string OriginalValue, string NewValue)> changes, string modifiedBy, CancellationToken cancellationToken = default);
    }
}