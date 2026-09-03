using System.ComponentModel.DataAnnotations;
using Backend.Features.Quizes;

namespace Backend.Features.Quizes.Dtos;

public class CreateQuizRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public Guid ThemeId { get; set; }

    [Range(1, 100)]
    public int QuestionsPerGame { get; set; } = 15;

    public QuestionCountMode QuestionCountMode { get; set; } = QuestionCountMode.HostSelectable;
}
