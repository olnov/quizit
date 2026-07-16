namespace Backend.Features.GameSessions;

public class GameSessionQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameSessionId { get; set; }
    public Guid QuestionId { get; set; }
    public int Order { get; set; }
}
