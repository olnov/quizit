namespace Backend.Features.GameSessions.Dtos;

public class PlayerStatisticsDto
{
    public string PlayerId { get; set; } = string.Empty;
    public int Score { get; set; }
    public List<PlayerStatisticsRowDto> Rows { get; set; } = new();
}

public class PlayerStatisticsRowDto
{
    public string Question { get; set; } = string.Empty;
    public string? PlayerAnswer { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? Explanation { get; set; }
}
