using IBS.DataAccess.Data;
using IBS.Models;
using Microsoft.AspNetCore.SignalR;

namespace IBSWeb.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly IServiceProvider _serviceProvider;

        public NotificationHub(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("OnConnected");
            await base.OnConnectedAsync();
        }

        public async Task SaveUserConnection(string username)
        {
            if (!string.IsNullOrEmpty(username))
            {
                var connectionId = Context.ConnectionId;

                // Create a new DbContext instance scoped to this method
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var hubConnection = new HubConnection
                    {
                        ConnectionId = connectionId,
                        UserName = username
                    };

                    dbContext.HubConnections.Add(hubConnection);
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Create a new DbContext instance scoped to this method
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var hubConnection =
                    dbContext.HubConnections.FirstOrDefault(con => con.ConnectionId == Context.ConnectionId);
                if (hubConnection != null)
                {
                    dbContext.HubConnections.Remove(hubConnection);
                    await dbContext.SaveChangesAsync();
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
