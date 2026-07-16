namespace Backend.Features.GameRooms.Dtos;

public class SubmitAnswerRequest
{
    public required string PlayerToken { get; set; }
    public Guid AnswerOptionId { get; set; }
}
