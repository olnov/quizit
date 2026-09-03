namespace Backend.Features.Quizes.Dtos;

using Backend.Features.Quizes;

public class QuizDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid ThemeId { get; set; }
    public int QuestionsPerGame { get; set; }
    public QuestionCountMode QuestionCountMode { get; set; }
    public int QuestionCount { get; set; }
}
