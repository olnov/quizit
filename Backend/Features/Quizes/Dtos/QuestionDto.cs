namespace Backend.Features.Quizes.Dtos;

public class QuestionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? CodeContext { get; set; }
    public int Difficulty { get; set; }
    public List<AnswerOptionDto> Options { get; set; } = new();
}
