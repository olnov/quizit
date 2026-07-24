namespace Backend.Features.Users.Dtos;

public class UserListItemDto
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public required IReadOnlyCollection<string> Roles { get; init; }
    public bool IsDisabled { get; init; }
}
