namespace Backend.Features.Quizes.Dtos;

public class AdminQuizDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public Guid ThemeId { get; init; }
    public string ThemeName { get; init; } = string.Empty;
    public int QuestionsPerGame { get; init; }
    public QuizStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public required List<AdminQuestionDto> Questions { get; init; }
}
