namespace Backend.Features.GameSessions;

public class GameSessionAnswer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameSessionId { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public Guid QuestionId { get; set; }
    public Guid AnswerOptionId { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public bool IsCorrect { get; set; }
    public int ScoreAwarded { get; set; }
}
