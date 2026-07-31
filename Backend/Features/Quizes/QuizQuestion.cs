namespace Backend.Features.Quizes;

public class QuizQuestion
{
    public Guid QuizId { get; set; }
    public Guid QuestionId { get; set; }

    public Quiz Quiz { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
