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
            QuestionCountMode = quiz.QuestionCountMode,
            QuestionCount = quiz.QuizQuestions.Count,
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
            CodeContext = question.CodeContext,
            Difficulty = question.Difficulty,
            Options = question.Options
                .OrderBy(option => option.DisplayOrder)
                .Select(option => new AnswerOptionDto
                {
                    Id = option.Id,
                    Text = option.Text,
                })
                .ToList(),
        };
    }
}
