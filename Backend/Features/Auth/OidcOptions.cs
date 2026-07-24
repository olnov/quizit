namespace Backend.Features.Auth;

public class OidcOptions
{
    public const string SectionName = "Oidc";

    public string Issuer { get; set; } = "http://localhost:5298";
    public string WebuiClientId { get; set; } = "quizit-webui";
    public string? SigningCertificateBase64 { get; set; }
    public string? SigningCertificatePassword { get; set; }
    public string? EncryptionKeyBase64 { get; set; }
    public bool AllowEphemeralCredentials { get; set; }
    public bool RequireHttps { get; set; } = true;
}
