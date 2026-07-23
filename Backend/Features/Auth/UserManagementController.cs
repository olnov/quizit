using Backend.Features.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Auth;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Policy = "Api", Roles = Roles.Admin)]
public class UserManagementController : ControllerBase
{
    private readonly UserManagementService _userManagementService;

    public UserManagementController(UserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = await _userManagementService.CreateUserAsync(
            request.Username,
            request.Email,
            request.Password,
            request.Role);

        return Created($"/api/v1/admin/users/{user.Id}", ToDto(user, request.Role));
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
