using Microsoft.AspNetCore.SignalR;

namespace VRF_API.Hubs
{
    public class NotifyHub : Hub
    {
        // Called from Flutter or JS client
        public async Task SendMessage(string user, string message)
        {
            // Broadcast to all clients
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
