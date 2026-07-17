using System.ComponentModel.DataAnnotations;
using Backend.Shared;

namespace Backend.Features.Quizes.Dtos;

public class CreateQuestionRequest
{
    [Required]
    [MaxLength(1_000)]
    public string Text { get; set; } = string.Empty;

    [MaxLength(5_000)]
    public string? CodeContext { get; set; }

    [MaxLength(2_000)]
    public string? Explanation { get; set; }

    [Range(0, 1_000)]
    [MultipleOf(100)]
    public int Difficulty { get; set; }

    [Required]
    public List<string> Options { get; set; } = new();

    [Range(0, 3)]
    public int CorrectOptionIndex { get; set; }
}
