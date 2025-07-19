using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.SignalR;

namespace IBSWeb.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly IHubConnectionRepository _hubConnectionRepository;

        public NotificationHub(IHubConnectionRepository hubConnectionRepository)
        {
            _hubConnectionRepository = hubConnectionRepository;
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
                await _hubConnectionRepository.SaveConnectionAsync(username, Context.ConnectionId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await _hubConnectionRepository.RemoveConnectionAsync(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
