using Microsoft.AspNetCore.Identity;

namespace Backend.Features.Auth;

public class UserManagementService
{
    private static readonly HashSet<string> AssignableRoles = new(StringComparer.Ordinal)
    {
        Roles.Admin,
        Roles.QuizAuthor,
    };

    private readonly UserManager<QuizUser> _userManager;

    public UserManagementService(UserManager<QuizUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<QuizUser> CreateUserAsync(
        string username,
        string email,
        string password,
        string role)
    {
        if (!AssignableRoles.Contains(role))
        {
            throw new ArgumentException($"Role '{role}' cannot be assigned.");
        }

        var user = new QuizUser
        {
            DisplayName = username,
            UserName = username,
            Email = email,
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw CreateIdentityException("create user", createResult);
        }

        var addRoleResult = await _userManager.AddToRoleAsync(user, role);
        if (!addRoleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            throw CreateIdentityException($"assign the {role} role", addRoleResult);
        }

        return user;
    }

    private static InvalidOperationException CreateIdentityException(
        string operation,
        IdentityResult result)
    {
        var errors = string.Join(
            ", ",
            result.Errors.Select(error => error.Description));

        return new InvalidOperationException($"Could not {operation}: {errors}");
    }
}
