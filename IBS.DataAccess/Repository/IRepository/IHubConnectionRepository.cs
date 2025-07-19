namespace IBS.DataAccess.Repository.IRepository
{
    public interface IHubConnectionRepository
    {
        Task SaveConnectionAsync(string username, string connectionId);
        Task RemoveConnectionAsync(string connectionId);
        Task RemoveConnectionsByUsernameAsync(string username);
    }
}
