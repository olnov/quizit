namespace Backend.Features.GameRooms.Dtos;

public class ScoreboardDto
{
    public string GameCode { get; set; } = string.Empty;
    public List<PlayerState> Players { get; set; } = new();
}
