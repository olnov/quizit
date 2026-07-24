namespace Backend.Features.Quizes.Dtos;

public class QuizListItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public Guid ThemeId { get; init; }
    public string ThemeName { get; init; } = string.Empty;
    public int QuestionsPerGame { get; init; }
    public int QuestionCount { get; init; }
    public QuizStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
