using Backend.Features.GameRooms;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

public class GameHub : Hub
{
    private readonly GameRoomService _gameRoomService;

    public GameHub(GameRoomService gameRoomService)
    {
        _gameRoomService = gameRoomService;
    }

    public async Task JoinGame(string gameCode, string playerName)
    {
        try
        {
            _gameRoomService.AddPlayer(gameCode, playerName, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, gameCode);
            await BroadcastLobbyUpdated(gameCode);
        }
        catch (KeyNotFoundException)
        {
            throw new HubException("Game room was not found.");
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var room = _gameRoomService.MarkPlayerDisconnected(Context.ConnectionId);
        if (room is not null)
        {
            await BroadcastLobbyUpdated(room.GameCode);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private Task BroadcastLobbyUpdated(string gameCode)
    {
        var room = _gameRoomService.GetRoom(gameCode)
            ?? throw new HubException("Game room was not found.");

        var players = room.Players.Select(player => new
        {
            player.PlayerId,
            player.Name,
            player.Score,
            player.IsConnected,
        });

        return Clients.Group(gameCode).SendAsync("LobbyUpdated", new
        {
            room.GameCode,
            room.Status,
            Players = players,
        });
    }
}
