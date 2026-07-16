namespace Backend.Features.GameRooms.Dtos;

public class RevealQuestionDto
{
    public Guid QuestionId { get; set; }
    public Guid CorrectOptionId { get; set; }
}
