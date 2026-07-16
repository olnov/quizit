namespace Backend.Features.Quizes;

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ThemeId { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionDifficulty Difficulty { get; set; }
    public List<AnswerOption> Options { get; set; } = new();
    public Guid CorrectOptionId { get; set; }
}
