using Backend.Data;
using Backend.Features.Quizes.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Quizes;

public class QuizDesigner(AppDbContext dbContext)
{
    public async Task<PagedResult<QuizListItemDto>> GetQuizesAsync(
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Quizes
            .AsNoTracking()
            .Where(quiz => !quiz.IsDeleted)
            .OrderBy(quiz => quiz.Title)
            .ThenBy(quiz => quiz.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(quiz => new QuizListItemDto
            {
                Id = quiz.Id,
                Title = quiz.Title,
                ThemeId = quiz.ThemeId,
                ThemeName = dbContext.QuizThemes
                    .Where(theme => theme.Id == quiz.ThemeId)
                    .Select(theme => theme.Name)
                    .FirstOrDefault() ?? string.Empty,
                QuestionsPerGame = quiz.QuestionsPerGame,
                QuestionCount = dbContext.Questions.Count(question => question.ThemeId == quiz.ThemeId),
                Status = quiz.Status,
                CreatedAt = quiz.CreatedAt,
                UpdatedAt = quiz.UpdatedAt,
            })
            .ToArrayAsync(cancellationToken);

        return new PagedResult<QuizListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<AdminQuizDto> GetQuizAsync(Guid quizId, CancellationToken cancellationToken)
    {
        var quiz = await GetRequiredQuizAsync(quizId, cancellationToken);
        return await ToAdminDtoAsync(quiz, cancellationToken);
    }

    public async Task<AdminQuizDto> CreateQuizAsync(
        CreateQuizRequest request,
        CancellationToken cancellationToken)
    {
        ValidateQuizMetadata(request.Title, request.QuestionsPerGame);
        await EnsureThemeExistsAsync(request.ThemeId, cancellationToken);

        var quiz = new Quiz
        {
            Title = request.Title.Trim(),
            ThemeId = request.ThemeId,
            QuestionsPerGame = request.QuestionsPerGame,
            Status = QuizStatus.Draft,
        };

        dbContext.Quizes.Add(quiz);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await ToAdminDtoAsync(quiz, cancellationToken);
    }

    public async Task<AdminQuizDto> UpdateQuizAsync(
        Guid quizId,
        UpdateQuizRequest request,
        CancellationToken cancellationToken)
    {
        ValidateQuizMetadata(request.Title, request.QuestionsPerGame);
        await EnsureThemeExistsAsync(request.ThemeId, cancellationToken);
        ValidateQuestions(request.Questions);

        var quiz = await GetRequiredQuizAsync(quizId, cancellationToken);
        var existingQuestions = await dbContext.Questions
            .Where(question => question.ThemeId == request.ThemeId)
            .Include(question => question.Options)
            .ToListAsync(cancellationToken);

        var existingById = existingQuestions.ToDictionary(question => question.Id);
        var requestQuestionIds = request.Questions
            .Where(question => question.Id.HasValue)
            .Select(question => question.Id!.Value)
            .ToList();

        if (requestQuestionIds.Count != requestQuestionIds.Distinct().Count())
        {
            throw new ArgumentException("A question can appear only once in an update request.");
        }

        if (requestQuestionIds.Any(id => !existingById.ContainsKey(id)))
        {
            throw new ArgumentException("All existing questions must belong to the selected quiz theme.");
        }

        var changedQuestions = request.Questions
            .Where(question => !question.Id.HasValue || HasQuestionChanged(existingById[question.Id.Value], question))
            .ToList();
        var removedQuestions = existingQuestions
            .Where(question => !requestQuestionIds.Contains(question.Id))
            .ToList();

        await EnsureQuestionsAreNotUsedInSessionsAsync(
            changedQuestions.Where(question => question.Id.HasValue).Select(question => question.Id!.Value)
                .Concat(removedQuestions.Select(question => question.Id)),
            cancellationToken);

        foreach (var questionRequest in changedQuestions)
        {
            if (!questionRequest.Id.HasValue)
            {
                dbContext.Questions.Add(CreateQuestion(request.ThemeId, questionRequest));
                continue;
            }

            var question = existingById[questionRequest.Id.Value];
            ApplyQuestion(question, questionRequest);
        }

        dbContext.Questions.RemoveRange(removedQuestions);

        quiz.Title = request.Title.Trim();
        quiz.ThemeId = request.ThemeId;
        quiz.QuestionsPerGame = request.QuestionsPerGame;
        quiz.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await ToAdminDtoAsync(quiz, cancellationToken);
    }

    public async Task<AdminQuizDto> PublishQuizAsync(Guid quizId, CancellationToken cancellationToken)
    {
        var quiz = await GetRequiredQuizAsync(quizId, cancellationToken);
        var questionCount = await dbContext.Questions
            .CountAsync(question => question.ThemeId == quiz.ThemeId, cancellationToken);

        if (questionCount < quiz.QuestionsPerGame)
        {
            throw new InvalidOperationException(
                "A quiz cannot be published until its theme has enough questions for one game.");
        }

        quiz.Status = QuizStatus.Published;
        quiz.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await ToAdminDtoAsync(quiz, cancellationToken);
    }

    public async Task<AdminQuizDto> ArchiveQuizAsync(Guid quizId, CancellationToken cancellationToken)
    {
        var quiz = await GetRequiredQuizAsync(quizId, cancellationToken);
        quiz.Status = QuizStatus.Archived;
        quiz.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await ToAdminDtoAsync(quiz, cancellationToken);
    }

    public async Task<AdminQuizDto> MoveQuizToDraftAsync(Guid quizId, CancellationToken cancellationToken)
    {
        var quiz = await GetRequiredQuizAsync(quizId, cancellationToken);
        quiz.Status = QuizStatus.Draft;
        quiz.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await ToAdminDtoAsync(quiz, cancellationToken);
    }

    public async Task DeleteQuizAsync(Guid quizId, CancellationToken cancellationToken)
    {
        var quiz = await GetRequiredQuizAsync(quizId, cancellationToken);

        if (await dbContext.GameSessions.AnyAsync(session => session.QuizId == quizId, cancellationToken))
        {
            throw new InvalidOperationException("A quiz with game sessions cannot be deleted.");
        }

        quiz.IsDeleted = true;
        quiz.DeletedAt = DateTime.UtcNow;
        quiz.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<QuizImportDto> ExportQuizAsync(Guid quizId, CancellationToken cancellationToken)
    {
        var quiz = await GetQuizAsync(quizId, cancellationToken);
        return new QuizImportDto
        {
            SchemaVersion = 1,
            Theme = quiz.ThemeName,
            Quiz = new QuizImportMetadataDto
            {
                Title = quiz.Title,
                QuestionsPerGame = quiz.QuestionsPerGame,
            },
            Questions = quiz.Questions.Select(question => new QuizImportQuestionDto
            {
                Text = question.Text,
                CodeContext = question.CodeContext,
                Explanation = question.Explanation,
                Difficulty = question.Difficulty,
                Options = question.Options,
                CorrectOptionIndex = question.CorrectOptionIndex,
            }).ToList(),
        };
    }

    public QuizImportValidationDto ValidateImport(QuizImportDto document)
    {
        var errors = new List<string>();

        if (document.SchemaVersion != 1)
        {
            errors.Add("schemaVersion must be 1.");
        }

        if (string.IsNullOrWhiteSpace(document.Theme))
        {
            errors.Add("Theme is required.");
        }

        if (string.IsNullOrWhiteSpace(document.Quiz?.Title))
        {
            errors.Add("Quiz title is required.");
        }

        if (document.Quiz is null || document.Quiz.QuestionsPerGame is < 1 or > 100)
        {
            errors.Add("questionsPerGame must be between 1 and 100.");
        }

        for (var index = 0; index < document.Questions.Count; index++)
        {
            try
            {
                ValidateQuestion(document.Questions[index]);
            }
            catch (ArgumentException exception)
            {
                errors.Add($"Question {index + 1}: {exception.Message}");
            }
        }

        if (document.Quiz is not null && document.Questions.Count < document.Quiz.QuestionsPerGame)
        {
            errors.Add("The document does not contain enough questions for one game.");
        }

        return new QuizImportValidationDto
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Preview = errors.Count == 0 && document.Quiz is not null
                ? new QuizImportPreviewDto
                {
                    Theme = document.Theme.Trim(),
                    Title = document.Quiz.Title.Trim(),
                    QuestionsPerGame = document.Quiz.QuestionsPerGame,
                    QuestionCount = document.Questions.Count,
                }
                : null,
        };
    }

    public async Task<AdminQuizDto> ImportQuizAsync(QuizImportDto document, CancellationToken cancellationToken)
    {
        var validation = ValidateImport(document);
        if (!validation.IsValid)
        {
            throw new ArgumentException(string.Join(" ", validation.Errors));
        }

        var themeName = document.Theme.Trim();
        if (await dbContext.QuizThemes.AnyAsync(theme => theme.Name == themeName, cancellationToken))
        {
            throw new InvalidOperationException($"A quiz theme named '{themeName}' already exists.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var theme = new QuizTheme { Name = themeName };
        var quiz = new Quiz
        {
            Title = document.Quiz.Title.Trim(),
            ThemeId = theme.Id,
            QuestionsPerGame = document.Quiz.QuestionsPerGame,
            Status = QuizStatus.Draft,
        };

        dbContext.QuizThemes.Add(theme);
        dbContext.Quizes.Add(quiz);
        foreach (var question in document.Questions)
        {
            dbContext.Questions.Add(CreateQuestion(theme.Id, question));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await ToAdminDtoAsync(quiz, cancellationToken);
    }

    public async Task<IReadOnlyCollection<QuizThemeDto>> GetThemesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.QuizThemes
            .AsNoTracking()
            .OrderBy(theme => theme.Name)
            .Select(theme => new QuizThemeDto { Id = theme.Id, Name = theme.Name })
            .ToListAsync(cancellationToken);
    }

    public async Task<QuizThemeDto> CreateThemeAsync(
        CreateQuizThemeRequest request,
        CancellationToken cancellationToken)
    {
        var name = ValidateThemeName(request.Name);
        if (await dbContext.QuizThemes.AnyAsync(theme => theme.Name == name, cancellationToken))
        {
            throw new InvalidOperationException($"A quiz theme named '{name}' already exists.");
        }

        var theme = new QuizTheme { Name = name };
        dbContext.QuizThemes.Add(theme);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new QuizThemeDto { Id = theme.Id, Name = theme.Name };
    }

    public async Task<QuizThemeDto> UpdateThemeAsync(
        Guid themeId,
        UpdateQuizThemeRequest request,
        CancellationToken cancellationToken)
    {
        var name = ValidateThemeName(request.Name);
        var theme = await GetRequiredThemeAsync(themeId, cancellationToken);
        if (await dbContext.QuizThemes.AnyAsync(
                current => current.Id != themeId && current.Name == name,
                cancellationToken))
        {
            throw new InvalidOperationException($"A quiz theme named '{name}' already exists.");
        }

        theme.Name = name;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new QuizThemeDto { Id = theme.Id, Name = theme.Name };
    }

    public async Task DeleteThemeAsync(Guid themeId, CancellationToken cancellationToken)
    {
        var theme = await GetRequiredThemeAsync(themeId, cancellationToken);
        var isUsed = await dbContext.Quizes.AnyAsync(quiz => quiz.ThemeId == themeId, cancellationToken)
            || await dbContext.Questions.AnyAsync(question => question.ThemeId == themeId, cancellationToken);

        if (isUsed)
        {
            throw new InvalidOperationException("A theme with quizzes or questions cannot be deleted.");
        }

        dbContext.QuizThemes.Remove(theme);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Quiz> GetRequiredQuizAsync(Guid quizId, CancellationToken cancellationToken)
    {
        return await dbContext.Quizes
            .SingleOrDefaultAsync(quiz => quiz.Id == quizId && !quiz.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Quiz with id '{quizId}' was not found.");
    }

    private async Task<QuizTheme> GetRequiredThemeAsync(Guid themeId, CancellationToken cancellationToken)
    {
        return await dbContext.QuizThemes.SingleOrDefaultAsync(theme => theme.Id == themeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Quiz theme with id '{themeId}' was not found.");
    }

    private async Task EnsureThemeExistsAsync(Guid themeId, CancellationToken cancellationToken)
    {
        _ = await GetRequiredThemeAsync(themeId, cancellationToken);
    }

    private async Task<AdminQuizDto> ToAdminDtoAsync(Quiz quiz, CancellationToken cancellationToken)
    {
        var theme = await GetRequiredThemeAsync(quiz.ThemeId, cancellationToken);
        var questions = await dbContext.Questions
            .AsNoTracking()
            .Where(question => question.ThemeId == quiz.ThemeId)
            .Include(question => question.Options)
            .OrderBy(question => question.Difficulty)
            .ThenBy(question => question.Text)
            .ToListAsync(cancellationToken);

        return new AdminQuizDto
        {
            Id = quiz.Id,
            Title = quiz.Title,
            ThemeId = quiz.ThemeId,
            ThemeName = theme.Name,
            QuestionsPerGame = quiz.QuestionsPerGame,
            Status = quiz.Status,
            CreatedAt = quiz.CreatedAt,
            UpdatedAt = quiz.UpdatedAt,
            Questions = questions.Select(ToAdminQuestionDto).ToList(),
        };
    }

    private static AdminQuestionDto ToAdminQuestionDto(Question question)
    {
        var options = question.Options.OrderBy(option => option.Id).ToList();
        var correctOptionIndex = options.FindIndex(option => option.Id == question.CorrectOptionId);
        if (correctOptionIndex < 0)
        {
            throw new InvalidOperationException($"Question with id '{question.Id}' has no valid correct answer.");
        }

        return new AdminQuestionDto
        {
            Id = question.Id,
            Text = question.Text,
            CodeContext = question.CodeContext,
            Explanation = question.Explanation,
            Difficulty = question.Difficulty,
            Options = options.Select(option => option.Text).ToList(),
            CorrectOptionIndex = correctOptionIndex,
        };
    }

    private async Task EnsureQuestionsAreNotUsedInSessionsAsync(
        IEnumerable<Guid> questionIds,
        CancellationToken cancellationToken)
    {
        var ids = questionIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        if (await dbContext.GameSessionQuestions
            .AnyAsync(sessionQuestion => ids.Contains(sessionQuestion.QuestionId), cancellationToken))
        {
            throw new InvalidOperationException(
                "Questions that have been used in a game session cannot be changed or deleted.");
        }
    }

    private static bool HasQuestionChanged(Question question, UpdateQuestionRequest request)
    {
        var options = question.Options.OrderBy(option => option.Id).ToList();
        var currentCorrectOptionIndex = options.FindIndex(option => option.Id == question.CorrectOptionId);
        return question.Text != request.Text.Trim()
            || question.CodeContext != NormalizeOptionalText(request.CodeContext)
            || question.Explanation != NormalizeOptionalText(request.Explanation)
            || question.Difficulty != request.Difficulty
            || currentCorrectOptionIndex != request.CorrectOptionIndex
            || options.Select(option => option.Text).SequenceEqual(request.Options.Select(option => option.Trim())) == false;
    }

    private static Question CreateQuestion(Guid themeId, UpdateQuestionRequest request)
    {
        var question = new Question { Id = Guid.NewGuid(), ThemeId = themeId };
        ApplyQuestion(question, request);
        return question;
    }

    private static Question CreateQuestion(Guid themeId, QuizImportQuestionDto request)
    {
        var question = new Question { Id = Guid.NewGuid(), ThemeId = themeId };
        ApplyQuestion(question, request.Text, request.CodeContext, request.Explanation,
            request.Difficulty, request.Options, request.CorrectOptionIndex);
        return question;
    }

    private static void ApplyQuestion(Question question, UpdateQuestionRequest request)
    {
        ApplyQuestion(question, request.Text, request.CodeContext, request.Explanation,
            request.Difficulty, request.Options, request.CorrectOptionIndex);
    }

    private static void ApplyQuestion(
        Question question,
        string text,
        string? codeContext,
        string? explanation,
        int difficulty,
        IReadOnlyList<string> optionTexts,
        int correctOptionIndex)
    {
        question.Text = text.Trim();
        question.CodeContext = NormalizeOptionalText(codeContext);
        question.Explanation = NormalizeOptionalText(explanation);
        question.Difficulty = difficulty;

        question.Options.Clear();
        foreach (var optionText in optionTexts)
        {
            question.Options.Add(new AnswerOption
            {
                QuestionId = question.Id,
                Text = optionText.Trim(),
            });
        }

        question.CorrectOptionId = question.Options[correctOptionIndex].Id;
    }

    private static void ValidateQuizMetadata(string title, int questionsPerGame)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Quiz title is required.");
        }

        if (questionsPerGame is < 1 or > 100)
        {
            throw new ArgumentException("questionsPerGame must be between 1 and 100.");
        }
    }

    private static void ValidateQuestions(IReadOnlyList<UpdateQuestionRequest> questions)
    {
        foreach (var question in questions)
        {
            ValidateQuestion(question.Text, question.Difficulty, question.Options, question.CorrectOptionIndex);
        }
    }

    private static void ValidateQuestion(QuizImportQuestionDto question)
    {
        ValidateQuestion(question.Text, question.Difficulty, question.Options, question.CorrectOptionIndex);
    }

    private static void ValidateQuestion(
        string text,
        int difficulty,
        IReadOnlyList<string> optionTexts,
        int correctOptionIndex)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text is required.");
        }

        if (difficulty is < 0 or > 1_000 || difficulty % 100 != 0)
        {
            throw new ArgumentException("Difficulty must be between 0 and 1000 in increments of 100.");
        }

        if (optionTexts.Count != 4 || optionTexts.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("A question must have exactly four non-empty answer options.");
        }

        if (correctOptionIndex is < 0 or > 3)
        {
            throw new ArgumentException("correctOptionIndex must be between 0 and 3.");
        }
    }

    private static string ValidateThemeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Theme name is required.");
        }

        return name.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public class PagedResult<T>
{
    public required T[] Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
