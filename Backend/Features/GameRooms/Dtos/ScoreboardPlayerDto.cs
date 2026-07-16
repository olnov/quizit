namespace Backend.Features.GameRooms.Dtos;

public class ScoreboardPlayerDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
}
