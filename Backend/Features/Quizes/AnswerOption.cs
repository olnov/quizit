namespace Backend.Features.Quizes;

public class AnswerOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = string.Empty;
}
