using Backend.Features.Quizes.Dtos;

namespace Backend.Features.Quizes;

public static class QuizMapper
{
    public static QuizDto ToDto(Quiz quiz)
    {
        return new QuizDto
        {
            Id = quiz.Id,
            Title = quiz.Title,
            ThemeId = quiz.ThemeId,
            QuestionsPerGame = quiz.QuestionsPerGame,
        };
    }

    public static QuizThemeDto ToDto(QuizTheme theme)
    {
        return new QuizThemeDto
        {
            Id = theme.Id,
            Name = theme.Name,
        };
    }

    public static QuestionDto ToDto(Question question)
    {
        return new QuestionDto
        {
            Id = question.Id,
            Text = question.Text,
            Difficulty = question.Difficulty,
            Options = question.Options.Select(option => new AnswerOptionDto
            {
                Id = option.Id,
                Text = option.Text,
            }).ToList(),
        };
    }
}
