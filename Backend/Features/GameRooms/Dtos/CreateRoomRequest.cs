namespace Backend.Features.GameRooms.Dtos;

public class CreateRoomRequest
{
    public Guid QuizId { get; set; }
    public required string HostName { get; set; }
    public required string ConnectionId { get; set; }
}
