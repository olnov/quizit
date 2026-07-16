namespace Backend.Features.GameRooms.Dtos;

public class CreateRoomResponseDto
{
    public GameRoomDto Room { get; set; } = new();
    public PlayerCredentialsDto Credentials { get; set; } = new();
}
