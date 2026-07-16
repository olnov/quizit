namespace Backend.Features.GameRooms.Dtos;

public class JoinGameRequest
{
    public string GameCode { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string? PlayerToken { get; set; }
}
