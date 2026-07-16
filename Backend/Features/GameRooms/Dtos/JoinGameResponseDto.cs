namespace Backend.Features.GameRooms.Dtos;

public class JoinGameResponseDto
{
    public GameRoomDto Room { get; set; } = new();
    public PlayerCredentialsDto Credentials { get; set; } = new();
}
