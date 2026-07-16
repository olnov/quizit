using Backend.Features.Quizes;

namespace Backend.Features.Quizes.Dtos;

public class QuestionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionDifficulty Difficulty { get; set; }
    public List<AnswerOptionDto> Options { get; set; } = new();
}
