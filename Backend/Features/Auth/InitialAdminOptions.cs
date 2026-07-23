namespace Backend.Features.Auth;

public class InitialAdminOptions
{
    public const string SectionName = "InitialAdmin";

    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}
