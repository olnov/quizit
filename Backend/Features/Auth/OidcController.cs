using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Backend.Features.Auth;

[ApiExplorerSettings(IgnoreApi = true)]
public class OidcController : Controller
{
    private readonly UserManager<QuizUser> _userManager;

    public OidcController(UserManager<QuizUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("~/connect/authorize")]
    [Authorize]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request is unavailable.");
        var user = await _userManager.GetUserAsync(User)
            ?? throw new InvalidOperationException("The authenticated user no longer exists.");

        return SignIn(
            await CreatePrincipalAsync(user, request.GetScopes()),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Exchange()
    {
        var result = await HttpContext.AuthenticateAsync(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        if (!result.Succeeded || result.Principal is null)
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var userId = result.Principal.GetClaim(OpenIddictConstants.Claims.Subject);
        var user = userId is null ? null : await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return SignIn(
            await CreatePrincipalAsync(user, result.Principal.GetScopes()),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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

        foreach (var role in await _userManager.GetRolesAsync(user))
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, role));
        }

        identity.SetDestinations(static claim => claim.Type switch
        {
            OpenIddictConstants.Claims.Subject => [OpenIddictConstants.Destinations.AccessToken],
            OpenIddictConstants.Claims.Name or OpenIddictConstants.Claims.Email =>
                [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            OpenIddictConstants.Claims.Role =>
                [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            _ => [],
        });

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopes);
        principal.SetResources("quizit-api");
        return principal;
    }
}
