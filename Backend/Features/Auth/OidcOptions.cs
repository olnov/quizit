namespace Backend.Features.Auth;

public class OidcOptions
{
    public const string SectionName = "Oidc";

    public string Issuer { get; set; } = "http://localhost:5298";
    public string WebuiClientId { get; set; } = "quizit-webui";
    public string WebuiRedirectUri { get; set; } = "http://localhost:5173/admin/auth/callback";
    public string WebuiPostLogoutRedirectUri { get; set; } = "http://localhost:5173/admin/login";
    public string? SigningCertificateBase64 { get; set; }
    public string? SigningCertificatePassword { get; set; }
    public string? EncryptionKeyBase64 { get; set; }
    public bool AllowEphemeralCredentials { get; set; }
}
