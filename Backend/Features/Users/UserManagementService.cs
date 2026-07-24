using Backend.Features.Auth;
using Backend.Features.Users.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Users;

public class UserManagementService(UserManager<QuizUser> userManager)
{
    private static readonly HashSet<string> AssignableRoles = new(StringComparer.Ordinal)
    {
        Roles.Admin,
        Roles.QuizAuthor,
    };

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

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw CreateIdentityException("create user", createResult);
        }

        var addRoleResult = await userManager.AddToRoleAsync(user, role);
        if (!addRoleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            throw CreateIdentityException($"assign the {role} role", addRoleResult);
        }

        return user;
    }

    public async Task<IReadOnlyCollection<UserListItemDto>> GetUsersAsync(
        CancellationToken cancellationToken)
    {
        var users = await userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.UserName)
            .ToListAsync(cancellationToken);

        var result = new List<UserListItemDto>(users.Count);
        foreach (var user in users)
        {
            result.Add(await ToListItemAsync(user));
        }

        return result;
    }

    public async Task<UserListItemDto> ChangeUserRoleAsync(
        string userId,
        string role,
        CancellationToken cancellationToken)
    {
        EnsureAssignableRole(role);
        var user = await GetRequiredUserAsync(userId);
        var currentRoles = await userManager.GetRolesAsync(user);

        if (currentRoles.Contains(role, StringComparer.Ordinal))
        {
            return await ToListItemAsync(user);
        }

        if (currentRoles.Contains(Roles.Admin, StringComparer.Ordinal) && role != Roles.Admin)
        {
            await EnsureAnotherAdministratorExistsAsync(user.Id);
        }

        var removeResult = await userManager.RemoveFromRolesAsync(
            user,
            currentRoles.Where(AssignableRoles.Contains));
        if (!removeResult.Succeeded)
        {
            throw CreateIdentityException("remove existing roles", removeResult);
        }

        var addResult = await userManager.AddToRoleAsync(user, role);
        if (!addResult.Succeeded)
        {
            throw CreateIdentityException($"assign the {role} role", addResult);
        }

        await UpdateSecurityStampAsync(user);
        return await ToListItemAsync(user);
    }

    public async Task<UserListItemDto> DisableUserAsync(
        string userId,
        string actingUserId,
        CancellationToken cancellationToken)
    {
        if (string.Equals(userId, actingUserId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Administrators cannot disable their own account.");
        }

        var user = await GetRequiredUserAsync(userId);
        if (await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            await EnsureAnotherAdministratorExistsAsync(user.Id);
        }

        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw CreateIdentityException("disable user", updateResult);
        }

        await UpdateSecurityStampAsync(user);
        return await ToListItemAsync(user);
    }

    private async Task<UserListItemDto> ToListItemAsync(QuizUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserListItemDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            Roles = roles.ToArray(),
            IsDisabled = await userManager.IsLockedOutAsync(user),
        };
    }

    private async Task<QuizUser> GetRequiredUserAsync(string userId)
    {
        return await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User with id '{userId}' was not found.");
    }

    private async Task EnsureAnotherAdministratorExistsAsync(string userId)
    {
        var administrators = await userManager.GetUsersInRoleAsync(Roles.Admin);
        if (!administrators.Any(administrator => administrator.Id != userId))
        {
            throw new InvalidOperationException("The last administrator cannot be changed or disabled.");
        }
    }

    private async Task UpdateSecurityStampAsync(QuizUser user)
    {
        var result = await userManager.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
        {
            throw CreateIdentityException("update the user's security stamp", result);
        }
    }

    private static void EnsureAssignableRole(string role)
    {
        if (!AssignableRoles.Contains(role))
        {
            throw new ArgumentException($"Role '{role}' cannot be assigned.");
        }
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
