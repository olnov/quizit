using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Backend.Features.Users;

namespace Backend.Features.Auth;

public class InitialAdminProvisioner
{
    private readonly InitialAdminOptions _options;
    private readonly UserManagementService _userManagementService;
    private readonly UserManager<QuizUser> _userManager;

    public InitialAdminProvisioner(
        IOptions<InitialAdminOptions> options,
        UserManagementService userManagementService,
        UserManager<QuizUser> userManager)
    {
        _options = options.Value;
        _userManagementService = userManagementService;
        _userManager = userManager;
    }

    public async Task ProvisionAsync()
    {
        var values = new[] { _options.Username, _options.Email, _options.Password };
        var hasAnyValue = values.Any(value => !string.IsNullOrWhiteSpace(value));

        if (!hasAnyValue)
        {
            return;
        }

        if (values.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException(
                "InitialAdmin requires Username, Email, and Password configuration values.");
        }

        var existingUser = await _userManager.FindByEmailAsync(_options.Email!);
        if (existingUser is null)
        {
            await _userManagementService.CreateUserAsync(
                _options.Username!,
                _options.Email!,
                _options.Password!,
                Roles.Admin);
            return;
        }

        if (await _userManager.IsInRoleAsync(existingUser, Roles.Admin))
        {
            return;
        }

        var addRoleResult = await _userManager.AddToRoleAsync(existingUser, Roles.Admin);
        if (!addRoleResult.Succeeded)
        {
            var errors = string.Join(
                ", ",
                addRoleResult.Errors.Select(error => error.Description));

            throw new InvalidOperationException(
                $"Could not assign the Admin role to the initial administrator: {errors}");
        }
    }
}
