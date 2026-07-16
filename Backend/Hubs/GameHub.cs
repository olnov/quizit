using Backend.Features.GameRooms;
using Backend.Features.GameRooms.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

public class GameHub : Hub
{
    private readonly GameRoomService _gameRoomService;

    public GameHub(GameRoomService gameRoomService)
    {
        _gameRoomService = gameRoomService;
    }

    public async Task<JoinGameResponseDto> JoinGame(JoinGameRequest request)
    {
        try
        {
            var player = _gameRoomService.JoinPlayer(
                request.GameCode,
                request.PlayerName,
                request.PlayerToken,
                Context.ConnectionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, request.GameCode);
            await BroadcastLobbyUpdated(request.GameCode);

            var room = _gameRoomService.GetRoom(request.GameCode)
                ?? throw new HubException("Game room was not found.");

            return new JoinGameResponseDto
            {
                Room = GameRoomMapper.ToDto(room),
                Credentials = GameRoomMapper.ToCredentials(player),
            };
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

        return Clients.Group(gameCode).SendAsync("LobbyUpdated", GameRoomMapper.ToDto(room));
    }
}
