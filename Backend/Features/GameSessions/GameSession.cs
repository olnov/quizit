namespace Backend.Features.GameSessions;

public class GameSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string GameRoomId { get; set; } = string.Empty;
    public Guid QuizId { get; set; }
    public GameSessionStatus Status { get; set; } = GameSessionStatus.InProgress;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public List<GameSessionPlayer> Players { get; set; } = new();
    public List<GameSessionQuestion> Questions { get; set; } = new();
}
