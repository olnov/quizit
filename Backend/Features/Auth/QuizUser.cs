using Microsoft.AspNetCore.Identity;
namespace Backend.Features.Auth;

public class QuizUser : IdentityUser
{
    public string? DisplayName { get; set; }
}