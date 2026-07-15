namespace Quizz.Backend.Features.GameRooms;

public class SubmittedAnswer
{
    public string PlayerId { get; set; } = string.Empty;
    public Guid QuestionId { get; set; }
    public Guid AnswerOptionId { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public bool IsCorrect { get; set; }
    public int ScoreAwarded { get; set; }
}
