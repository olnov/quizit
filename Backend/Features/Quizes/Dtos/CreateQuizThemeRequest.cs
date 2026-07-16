using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Quizes.Dtos;

public class CreateQuizThemeRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
