using Backend.Features.GameRooms;
using Backend.Features.GameRooms.Dtos;
using Backend.Features.GameSessions;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

public class GameHub : Hub
{
    private readonly GameRoomService _gameRoomService;
    private readonly GameSessionService _gameSessionService;

    public GameHub(GameRoomService gameRoomService, GameSessionService gameSessionService)
    {
        _gameRoomService = gameRoomService;
        _gameSessionService = gameSessionService;
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
            if (_gameRoomService.TryRevealWhenAllConnectedPlayersAnswered(room))
            {
                var reveal = await _gameSessionService.GetRevealAsync(room, Context.ConnectionAborted);
                await Clients.Group(room.GameCode).SendAsync("QuestionRevealed", reveal);
            }

            await BroadcastLobbyUpdated(room.GameCode);
            await Clients.Group(room.GameCode).SendAsync("RoomUpdated", GameRoomMapper.ToDto(room));
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
