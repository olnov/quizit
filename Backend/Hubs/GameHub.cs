using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

public class GameHub : Hub
{
    public Task JoinGame(string gameCode)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, gameCode);
    }
}