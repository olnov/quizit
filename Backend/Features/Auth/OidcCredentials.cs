using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Features.Auth;

public sealed class OidcCredentials
{
    public required X509Certificate2 SigningCertificate { get; init; }
    public required SymmetricSecurityKey EncryptionKey { get; init; }

    public static OidcCredentials Create(OidcOptions options, bool isProduction)
    {
        if (!string.IsNullOrWhiteSpace(options.SigningCertificateBase64)
            && !string.IsNullOrWhiteSpace(options.EncryptionKeyBase64))
        {
            return new OidcCredentials
            {
                SigningCertificate = X509CertificateLoader.LoadPkcs12(
                    Convert.FromBase64String(options.SigningCertificateBase64),
                    options.SigningCertificatePassword,
                    X509KeyStorageFlags.EphemeralKeySet),
                EncryptionKey = new SymmetricSecurityKey(
                    Convert.FromBase64String(options.EncryptionKeyBase64)),
            };
        }

        if (isProduction && !options.AllowEphemeralCredentials)
        {
            throw new InvalidOperationException(
                "Oidc requires SigningCertificateBase64 and EncryptionKeyBase64 in production.");
        }

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=QuizIt Development OIDC Signing",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature,
            critical: true));

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(7));

        return new OidcCredentials
        {
            SigningCertificate = certificate,
            EncryptionKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)),
        };
    }
}
