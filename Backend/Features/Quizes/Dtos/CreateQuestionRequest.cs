using System.ComponentModel.DataAnnotations;
using Backend.Features.Quizes;

namespace Backend.Features.Quizes.Dtos;

public class CreateQuestionRequest
{
    [Required]
    [MaxLength(1_000)]
    public string Text { get; set; } = string.Empty;

    public QuestionDifficulty Difficulty { get; set; }

    [Required]
    public List<string> Options { get; set; } = new();

    [Range(0, 3)]
    public int CorrectOptionIndex { get; set; }
}
