namespace Quizz.Backend.Features.GameRooms;

public class ScoreboardDto
{
    public string GameCode { get; set; } = string.Empty;
    public List<PlayerState> Players { get; set; } = new();
}
