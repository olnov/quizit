using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Quizes.Dtos;

public class UpdateQuizThemeRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
