using Backend.Features.GameSessions;
using Backend.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Features.GameRooms;

public class GameQuestionTimeoutService : BackgroundService
{
    private readonly GameRoomService _gameRoomService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<GameHub> _gameHubContext;

    public GameQuestionTimeoutService(
        GameRoomService gameRoomService,
        IServiceScopeFactory scopeFactory,
        IHubContext<GameHub> gameHubContext)
    {
        _gameRoomService = gameRoomService;
        _scopeFactory = scopeFactory;
        _gameHubContext = gameHubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var room in _gameRoomService.GetTimedOutQuestions())
            {
                if (!_gameRoomService.TryRevealTimedOutQuestion(room))
                {
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                var gameSessionService = scope.ServiceProvider.GetRequiredService<GameSessionService>();
                var reveal = await gameSessionService.GetRevealAsync(room, stoppingToken);
                await _gameHubContext.Clients.Group(room.GameCode)
                    .SendAsync("QuestionRevealed", reveal, stoppingToken);
                await _gameHubContext.Clients.Group(room.GameCode)
                    .SendAsync("RoomUpdated", GameRoomMapper.ToDto(room), stoppingToken);
            }
        }
    }
}
