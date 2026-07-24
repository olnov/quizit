using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Backend.Features.Auth;

[ApiExplorerSettings(IgnoreApi = true)]
public class OidcController(UserManager<QuizUser> userManager) : Controller
{
    [HttpPost("~/api/v1/connect/token")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request is unavailable.");

        QuizUser? user;
        if (request.IsPasswordGrantType())
        {
            user = await userManager.FindByNameAsync(request.Username ?? string.Empty);
            if (user is null
                || await userManager.IsLockedOutAsync(user)
                || !await userManager.CheckPasswordAsync(user, request.Password ?? string.Empty))
            {
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
        }
        else if (request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal is null)
            {
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var userId = result.Principal.GetClaim(OpenIddictConstants.Claims.Subject);
            user = userId is null ? null : await userManager.FindByIdAsync(userId);
            if (user is null || await userManager.IsLockedOutAsync(user))
            {
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
        }
        else
        {
            throw new InvalidOperationException("Only password and refresh-token grants are supported.");
        }

        return SignIn(
            await CreatePrincipalAsync(user, request.GetScopes()),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<ClaimsPrincipal> CreatePrincipalAsync(QuizUser user, IEnumerable<string> scopes)
    {
        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        identity.SetClaim(OpenIddictConstants.Claims.Subject, user.Id);
        identity.SetClaim(OpenIddictConstants.Claims.Name, user.DisplayName ?? user.UserName);
        identity.SetClaim(OpenIddictConstants.Claims.Email, user.Email);

        foreach (var role in await userManager.GetRolesAsync(user))
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, role));
        }

        identity.SetDestinations(static claim => claim.Type switch
        {
            OpenIddictConstants.Claims.Subject => [OpenIddictConstants.Destinations.AccessToken],
            OpenIddictConstants.Claims.Name or OpenIddictConstants.Claims.Email or OpenIddictConstants.Claims.Role =>
                [OpenIddictConstants.Destinations.AccessToken],
            _ => [],
        });

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopes);
        principal.SetResources("quizit-api");
        return principal;
    }
}
