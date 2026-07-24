using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Quizes.Dtos;

public class QuizImportDto
{
    public int SchemaVersion { get; set; }

    [Required]
    [MaxLength(100)]
    public string Theme { get; set; } = string.Empty;

    [Required]
    public QuizImportMetadataDto Quiz { get; set; } = new();

    [Required]
    public List<QuizImportQuestionDto> Questions { get; set; } = new();
}

public class QuizImportMetadataDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 100)]
    public int QuestionsPerGame { get; set; }
}

public class QuizImportQuestionDto
{
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

public class QuizImportValidationDto
{
    public bool IsValid { get; init; }
    public required List<string> Errors { get; init; }
    public QuizImportPreviewDto? Preview { get; init; }
}

public class QuizImportPreviewDto
{
    public string Theme { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int QuestionsPerGame { get; init; }
    public int QuestionCount { get; init; }
}
