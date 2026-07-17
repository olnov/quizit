using System.ComponentModel.DataAnnotations;

namespace Backend.Features.GameRooms.Dtos;

public class UpdateRoomSettingsRequest
{
    public required string PlayerToken { get; set; }
    [Range(1, 100)]
    public int QuestionCount { get; set; }
    [Range(5, 3600)]
    public int? AnswerTimeLimitSeconds { get; set; }
}
