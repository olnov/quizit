namespace Backend.Features.Quizes.Dtos;

public class AdminQuestionDto
{
    public Guid Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public string? CodeContext { get; init; }
    public string? Explanation { get; init; }
    public int Difficulty { get; init; }
    public required List<string> Options { get; init; }
    public int CorrectOptionIndex { get; init; }
}
