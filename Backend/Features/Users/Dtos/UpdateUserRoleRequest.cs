using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Users.Dtos;

public class UpdateUserRoleRequest
{
    [Required]
    public string Role { get; set; } = string.Empty;
}
