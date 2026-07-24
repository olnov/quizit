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
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = _options.WebuiClientId,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            DisplayName = "QuizIt Web UI",
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.Password,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess,
                OpenIddictConstants.Permissions.Prefixes.Scope + "quizit_api",
            },
        };

        var application = await _applicationManager.FindByClientIdAsync(_options.WebuiClientId);
        if (application is null)
        {
            await _applicationManager.CreateAsync(descriptor);
            return;
        }

        await _applicationManager.UpdateAsync(application, descriptor);
    }
}
