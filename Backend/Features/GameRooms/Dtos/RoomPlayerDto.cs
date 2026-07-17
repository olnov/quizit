namespace Backend.Features.GameRooms.Dtos;

public class RoomPlayerDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public bool IsConnected { get; set; }
    public bool HasAnswered { get; set; }
}
