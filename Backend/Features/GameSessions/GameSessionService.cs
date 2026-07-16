using Backend.Features.GameRooms;

namespace Backend.Features.GameSessions;

public class GameSessionService
{
    public GameSession CreateFromRoom(GameRoom room)
    {
        return new GameSession
        {
            GameRoomId = room.GameId,
            QuizId = room.QuizId,
            Players = room.Players
                .Select(player => new GameSessionPlayer
                {
                    PlayerId = player.PlayerId,
                    Name = player.Name,
                    Score = player.Score,
                })
                .ToList(),
        };
    }

    private static GameSession UpdateStatus(GameSession gameSession, GameSessionStatus status)
    {
        gameSession.Status = status;
        return gameSession;
    }

    public GameSession Complete(GameSession gameSession)
    {
        gameSession.Status = GameSessionStatus.Completed;
        gameSession.CompletedAt = DateTime.UtcNow;
        return gameSession;
    }

    public GameSession Cancel(GameSession gameSession)
    {
        return UpdateStatus(gameSession, GameSessionStatus.Cancelled);
    }

    // public GameSession GetGameSession(Guid id)
    // {
    //     return ;
    // }
}
