using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Quizes.Dtos;

public class UpdateQuizRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public Guid ThemeId { get; set; }

    [Range(1, 100)]
    public int QuestionsPerGame { get; set; }

    [Required]
    public List<UpdateQuestionRequest> Questions { get; set; } = new();
}

public class UpdateQuestionRequest
{
    public Guid? Id { get; set; }

    [Required]
    [MaxLength(1_000)]
    public string Text { get; set; } = string.Empty;

    [MaxLength(5_000)]
    public string? CodeContext { get; set; }

    [MaxLength(2_000)]
    public string? Explanation { get; set; }

    [Range(0, 1_000)]
    public int Difficulty { get; set; }

    [Required]
    public List<string> Options { get; set; } = new();

    [Range(0, 3)]
    public int CorrectOptionIndex { get; set; }
}
