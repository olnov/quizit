namespace Backend.Features.GameRooms.Dtos;

public class GameRoomDto
{
    public string GameCode { get; set; } = string.Empty;
    public Guid QuizId { get; set; }
    public GameStatus Status { get; set; }
    public int CurrentQuestionIndex { get; set; }
    public List<RoomPlayerDto> Players { get; set; } = new();
}
