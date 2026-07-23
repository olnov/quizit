using OpenIddict.Abstractions;

namespace Backend.Features.Auth;

public class OidcClientProvisioner
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly OidcOptions _options;

    public OidcClientProvisioner(
        IOpenIddictApplicationManager applicationManager,
        Microsoft.Extensions.Options.IOptions<OidcOptions> options)
    {
        _applicationManager = applicationManager;
        _options = options.Value;
    }

    public async Task ProvisionAsync()
    {
        if (await _applicationManager.FindByClientIdAsync(_options.WebuiClientId) is not null)
        {
            return;
        }

        await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = _options.WebuiClientId,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            DisplayName = "QuizIt Web UI",
            RedirectUris = { new Uri(_options.WebuiRedirectUri) },
            PostLogoutRedirectUris = { new Uri(_options.WebuiPostLogoutRedirectUri) },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.EndSession,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess,
                OpenIddictConstants.Permissions.Prefixes.Scope + "quizit_api",
            },
        });
    }
}
