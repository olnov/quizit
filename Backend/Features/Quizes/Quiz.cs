namespace Backend.Features.Quizes;

public class Quiz
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public Guid ThemeId { get; set; }
    public int QuestionsPerGame { get; set; } = 15;
}
