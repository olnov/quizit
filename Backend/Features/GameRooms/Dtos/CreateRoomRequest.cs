using System.ComponentModel.DataAnnotations;
using Backend.Shared;

namespace Backend.Features.GameRooms.Dtos;

public class CreateRoomRequest
{
    public Guid QuizId { get; set; }
    public required string HostName { get; set; }
    [Range(1, 100)]
    public int QuestionCount { get; set; }
    [Range(5, 3600)]
    public int? AnswerTimeLimitSeconds { get; set; }
    public QuestionSelectionMode QuestionSelectionMode { get; set; } = QuestionSelectionMode.AscendingDifficulty;
    [Range(0, 1_000)]
    [MultipleOf(100)]
    public int? SpecificDifficulty { get; set; }
}
