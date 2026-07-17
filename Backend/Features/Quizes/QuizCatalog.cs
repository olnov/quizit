using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Quizes;

public class QuizCatalog
{
    private readonly AppDbContext _dbContext;

    public QuizCatalog(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<QuizTheme>> GetThemesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.QuizThemes
            .OrderBy(theme => theme.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<QuizTheme> CreateThemeAsync(string name, CancellationToken cancellationToken)
    {
        var theme = new QuizTheme { Name = name.Trim() };
        _dbContext.QuizThemes.Add(theme);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return theme;
    }

    public async Task<IReadOnlyCollection<Question>> GetQuestionsAsync(
        Guid themeId,
        CancellationToken cancellationToken)
    {
        await EnsureThemeExistsAsync(themeId, cancellationToken);

        return await _dbContext.Questions
            .Where(question => question.ThemeId == themeId)
            .Include(question => question.Options)
            .ToListAsync(cancellationToken);
    }

    public async Task<Question> CreateQuestionAsync(
        Guid themeId,
        string text,
        string? codeContext,
        string? explanation,
        QuestionDifficulty difficulty,
        IReadOnlyList<string> optionTexts,
        int correctOptionIndex,
        CancellationToken cancellationToken)
    {
        await EnsureThemeExistsAsync(themeId, cancellationToken);

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

        var questionId = Guid.NewGuid();
        var options = optionTexts
            .Select(optionText => new AnswerOption
            {
                QuestionId = questionId,
                Text = optionText.Trim(),
            })
            .ToList();

        var question = new Question
        {
            Id = questionId,
            ThemeId = themeId,
            Text = text.Trim(),
            CodeContext = string.IsNullOrWhiteSpace(codeContext) ? null : codeContext.Trim(),
            Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim(),
            Difficulty = difficulty,
            Options = options,
            CorrectOptionId = options[correctOptionIndex].Id,
        };

        _dbContext.Questions.Add(question);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return question;
    }

    public async Task<IReadOnlyCollection<Quiz>> GetQuizesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Quizes
            .OrderBy(quiz => quiz.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<Quiz> CreateQuizAsync(
        string title,
        Guid themeId,
        int questionsPerGame,
        CancellationToken cancellationToken)
    {
        await EnsureThemeExistsAsync(themeId, cancellationToken);

        var quiz = new Quiz
        {
            Title = title.Trim(),
            ThemeId = themeId,
            QuestionsPerGame = questionsPerGame,
        };

        _dbContext.Quizes.Add(quiz);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return quiz;
    }

    private async Task EnsureThemeExistsAsync(Guid themeId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.QuizThemes.AnyAsync(theme => theme.Id == themeId, cancellationToken))
        {
            throw new KeyNotFoundException($"Quiz theme with id '{themeId}' was not found.");
        }
    }
}
