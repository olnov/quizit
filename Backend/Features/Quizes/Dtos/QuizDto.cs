namespace Backend.Features.Quizes.Dtos;

public class QuizDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid ThemeId { get; set; }
    public int QuestionsPerGame { get; set; }
}
