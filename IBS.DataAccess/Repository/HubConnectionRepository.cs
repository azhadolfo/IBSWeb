using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository
{
    public class HubConnectionRepository : IHubConnectionRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public HubConnectionRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveConnectionAsync(string username, string connectionId)
        {
            var connection = new HubConnection
            {
                UserName = username,
                ConnectionId = connectionId
            };

            _dbContext.HubConnections.Add(connection);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveConnectionAsync(string connectionId)
        {
            var connection = await _dbContext.HubConnections
                .FirstOrDefaultAsync(c => c.ConnectionId == connectionId);

            if (connection != null)
            {
                _dbContext.HubConnections.Remove(connection);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task RemoveConnectionsByUsernameAsync(string username)
        {
            await _dbContext.HubConnections
                .Where(c => c.UserName == username)
                .ExecuteDeleteAsync(); 

            await _dbContext.SaveChangesAsync();
        }
    }
}
