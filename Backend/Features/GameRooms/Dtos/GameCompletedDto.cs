namespace Backend.Features.GameRooms.Dtos;

public class GameCompletedDto
{
    public string GameCode { get; set; } = string.Empty;
    public List<ScoreboardPlayerDto> Players { get; set; } = new();
}
