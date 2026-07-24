using Backend.Features.Auth;
using Backend.Features.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

namespace Backend.Features.Users;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Policy = "Api", Roles = Roles.Admin)]
public class UserManagementController(UserManagementService userManagementService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<UserListItemDto>>> GetUsers(
        CancellationToken cancellationToken)
    {
        return Ok(await userManagementService.GetUsersAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = await userManagementService.CreateUserAsync(
            request.Username,
            request.Email,
            request.Password,
            request.Role);

        return Created($"/api/v1/admin/users/{user.Id}", ToDto(user, request.Role));
    }

    [HttpPut("{userId}/role")]
    public async Task<ActionResult<UserListItemDto>> ChangeUserRole(
        string userId,
        [FromBody] UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await userManagementService.ChangeUserRoleAsync(
            userId,
            request.Role,
            cancellationToken));
    }

    [HttpPost("{userId}/disable")]
    public async Task<ActionResult<UserListItemDto>> DisableUser(
        string userId,
        CancellationToken cancellationToken)
    {
        var actingUserId = User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("The authenticated user identifier is missing.");

        return Ok(await userManagementService.DisableUserAsync(
            userId,
            actingUserId,
            cancellationToken));
    }

    private static UserDto ToDto(QuizUser user, string role)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            Role = role,
        };
    }
}
