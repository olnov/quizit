namespace Backend.Features.Quizes;

public class QuizCatalog
{
    private readonly Dictionary<Guid, QuizTheme> _themes = new();
    private readonly Dictionary<Guid, Question> _questions = new();
    private readonly Dictionary<Guid, Quiz> _quizes = new();

    public IReadOnlyCollection<QuizTheme> GetThemes() => _themes.Values.ToList();

    public QuizTheme CreateTheme(string name)
    {
        var theme = new QuizTheme { Name = name.Trim() };
        _themes.Add(theme.Id, theme);
        return theme;
    }

    public IReadOnlyCollection<Question> GetQuestions(Guid themeId)
    {
        EnsureThemeExists(themeId);
        return _questions.Values.Where(question => question.ThemeId == themeId).ToList();
    }

    public Question CreateQuestion(
        Guid themeId,
        string text,
        QuestionDifficulty difficulty,
        IReadOnlyList<string> optionTexts,
        int correctOptionIndex)
    {
        EnsureThemeExists(themeId);

        if (optionTexts.Count != 4)
        {
            throw new ArgumentException("A question must have exactly four answer options.");
        }

        if (correctOptionIndex is < 0 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(correctOptionIndex));
        }

        if (optionTexts.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Answer options cannot be empty.");
        }

        var options = optionTexts
            .Select(optionText => new AnswerOption { Text = optionText.Trim() })
            .ToList();

        var question = new Question
        {
            ThemeId = themeId,
            Text = text.Trim(),
            Difficulty = difficulty,
            Options = options,
            CorrectOptionId = options[correctOptionIndex].Id,
        };

        _questions.Add(question.Id, question);
        return question;
    }

    public IReadOnlyCollection<Quiz> GetQuizes() => _quizes.Values.ToList();

    public Quiz CreateQuiz(string title, Guid themeId, int questionsPerGame)
    {
        EnsureThemeExists(themeId);

        var quiz = new Quiz
        {
            Title = title.Trim(),
            ThemeId = themeId,
            QuestionsPerGame = questionsPerGame,
        };

        _quizes.Add(quiz.Id, quiz);
        return quiz;
    }

    private void EnsureThemeExists(Guid themeId)
    {
        if (!_themes.ContainsKey(themeId))
        {
            throw new KeyNotFoundException($"Quiz theme with id '{themeId}' was not found.");
        }
    }
}
