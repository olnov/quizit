namespace Backend.Features.GameRooms.Dtos;

public class GameRoomDto
{
    public string GameCode { get; set; } = string.Empty;
    public Guid QuizId { get; set; }
    public GameStatus Status { get; set; }
    public DateTime LobbyExpiresAt { get; set; }
    public int QuestionCount { get; set; }
    public int? AnswerTimeLimitSeconds { get; set; }
    public QuestionSelectionMode QuestionSelectionMode { get; set; }
    public int? SpecificDifficulty { get; set; }
    public DateTime? AnswerDeadlineAt { get; set; }
    public int CurrentQuestionIndex { get; set; }
    public List<RoomPlayerDto> Players { get; set; } = new();
}
