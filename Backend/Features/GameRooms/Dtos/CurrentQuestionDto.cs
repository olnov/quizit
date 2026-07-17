using Backend.Features.Quizes.Dtos;

namespace Backend.Features.GameRooms.Dtos;

public class CurrentQuestionDto
{
    public int Index { get; set; }
    public DateTime? AnswerDeadlineAt { get; set; }
    public QuestionDto Question { get; set; } = new();
}
