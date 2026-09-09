namespace Backend.Features.GameRooms.Dtos;

public class GameStateDto
{
    public GameRoomDto Room { get; set; } = new();
    public CurrentQuestionDto? Question { get; set; }
    public RevealQuestionDto? Reveal { get; set; }
    public Guid? SelectedOptionId { get; set; } 
}