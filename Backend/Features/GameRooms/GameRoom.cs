namespace Backend.Features.GameRooms;

public class GameRoom
{
    public string GameId { get; set; } = Guid.NewGuid().ToString();
    public string GameCode { get; set; } = string.Empty;
    public Guid QuizId { get; set; }
    public GameStatus Status { get; set; } = GameStatus.Waiting;
    public string HostPlayerId { get; set; } = string.Empty;
    public Guid? GameSessionId { get; set; }
    public List<PlayerState> Players { get; set; } = new();
    public List<Guid> QuestionIds { get; set; } = new();
    public int CurrentQuestionIndex { get; set; } = -1;
    public Dictionary<string, SubmittedAnswer> CurrentAnswers { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
