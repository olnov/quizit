namespace Backend.Features.GameRooms;

public class GameRoomCleanupService : BackgroundService
{
    private readonly GameRoomService _gameRoomService;

    public GameRoomCleanupService(GameRoomService gameRoomService)
    {
        _gameRoomService = gameRoomService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            _gameRoomService.CleanupExpiredRooms();
        }
    }
}
