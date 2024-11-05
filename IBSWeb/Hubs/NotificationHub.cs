using IBS.DataAccess.Data;
using IBS.Models;
using Microsoft.AspNetCore.SignalR;

namespace IBSWeb.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly ApplicationDbContext _dbContext;

        public NotificationHub(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public override Task OnConnectedAsync()
        {
            Clients.Caller.SendAsync("OnConnected");
            return base.OnConnectedAsync();
        }

        public async Task SaveUserConnection(string username)
        {
            if (!string.IsNullOrEmpty(username))
            {
                var connectionId = Context.ConnectionId;
                HubConnection hubConnection = new HubConnection
                {
                    ConnectionId = connectionId,
                    UserName = username
                };

                _dbContext.HubConnections.Add(hubConnection);
                await _dbContext.SaveChangesAsync();
            }
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var hubConnection = _dbContext.HubConnections.FirstOrDefault(con => con.ConnectionId == Context.ConnectionId);
            if (hubConnection != null)
            {
                _dbContext.HubConnections.Remove(hubConnection);
                _dbContext.SaveChangesAsync();
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}