namespace Quizz.Backend.Features.GameRooms;

public class CreateRoomRequest
{
    public Guid QuizId { get; set; }
    public string HostName { get; set; }
    public string ConnectionId { get; set; }
}