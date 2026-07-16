namespace Backend.Features.GameRooms;

public class PlayerState
{
    public string PlayerId { get; set; } = Guid.NewGuid().ToString();
    public string PlayerToken { get; set; } = Guid.NewGuid().ToString("N");
    public string ConnectionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; } = 0;
    public bool IsConnected { get; set; } = true;
}
