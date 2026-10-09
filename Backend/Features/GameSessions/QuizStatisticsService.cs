using Backend.Data;
using Backend.Features.GameSessions.Dtos;
using Backend.Features.Quizes;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.GameSessions;

public sealed class QuizStatisticsService(AppDbContext db, GameSessionService gameSessionService)
{
    public async Task<QuizStatisticsReportDto> GetReportAsync(
        Guid quizId, DateTime from, DateTime to, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (from.Kind != DateTimeKind.Utc || to.Kind != DateTimeKind.Utc || from >= to)
            throw new ArgumentException("A valid UTC time interval is required.");

        var quiz = await db.Quizes.AsNoTracking().SingleOrDefaultAsync(q => q.Id == quizId, cancellationToken)
            ?? throw new KeyNotFoundException($"Quiz with id '{quizId}' was not found.");
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Competition rooms require at least two players before starting. The persisted
        // session has no IsSolo column, so participant count identifies solo sessions.
        var sessions = db.GameSessions.AsNoTracking().Where(s =>
            s.QuizId == quizId && s.Status == GameSessionStatus.Completed
            && s.CompletedAt >= from && s.CompletedAt < to && s.Players.Count == 1);
        var totalCount = await sessions.CountAsync(cancellationToken);
        var averageScore = await sessions.SelectMany(s => s.Players)
            .AverageAsync(p => (double?)p.Score, cancellationToken) ?? 0;
        var items = await sessions.OrderByDescending(s => s.CompletedAt).ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new QuizAttemptDto(
                s.Id,
                s.Players.Select(p => p.Name).First(),
                s.CompletedAt!.Value,
                s.Players.Select(p => p.Score).First(),
                s.Questions.Count,
                s.Answers.Count,
                s.Answers.Count(a => a.IsCorrect)))
            .ToListAsync(cancellationToken);

        return new QuizStatisticsReportDto(quiz.Id, quiz.Title, totalCount, averageScore,
            page, pageSize, (int)Math.Ceiling((double)totalCount / pageSize), items);
    }

    public async Task<QuizAttemptDetailDto> GetDetailAsync(
        Guid quizId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await db.GameSessions.AsNoTracking().Include(s => s.Players)
            .SingleOrDefaultAsync(s => s.Id == sessionId && s.QuizId == quizId
                && s.Status == GameSessionStatus.Completed, cancellationToken);
        if (session is null || session.Players.Count != 1)
            throw new KeyNotFoundException($"Solo game session with id '{sessionId}' was not found.");

        var player = session.Players[0];
        var statistics = await gameSessionService.GetPlayerStatisticsAsync(
            sessionId, player.PlayerId, cancellationToken);
        return new QuizAttemptDetailDto(session.Id, player.Name, session.CompletedAt!.Value,
            statistics.Score, statistics.Rows);
    }

    public async Task<QuizDetailedReportDto> GetDetailedReportAsync(
        Guid quizId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        if (from.Kind != DateTimeKind.Utc || to.Kind != DateTimeKind.Utc || from >= to)
            throw new ArgumentException("A valid UTC time interval is required.");

        var quiz = await db.Quizes.AsNoTracking().SingleOrDefaultAsync(q => q.Id == quizId, cancellationToken)
            ?? throw new KeyNotFoundException($"Quiz with id '{quizId}' was not found.");
        var sessions = db.GameSessions.AsNoTracking().Where(s =>
            s.QuizId == quizId && s.Status == GameSessionStatus.Completed
            && s.CompletedAt >= from && s.CompletedAt < to && s.Players.Count == 1);

        var completedGames = 0;
        var validDurationCount = 0;
        var totalDurationSeconds = 0.0;
        await foreach (var session in sessions.Select(s => new { s.StartedAt, s.CompletedAt })
            .AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            completedGames++;
            if (session.CompletedAt >= session.StartedAt)
            {
                validDurationCount++;
                totalDurationSeconds += (session.CompletedAt!.Value - session.StartedAt).TotalSeconds;
            }
        }

        var questionStats = new Dictionary<Guid, QuestionAggregate>();
        var answerQuery =
            from answer in db.GameSessionAnswers.AsNoTracking()
            join session in sessions on answer.GameSessionId equals session.Id
            join question in db.GameSessionQuestions.AsNoTracking()
                on new { answer.GameSessionId, answer.QuestionId }
                equals new { question.GameSessionId, question.QuestionId }
            select new { answer.QuestionId, answer.IsCorrect, answer.SubmittedAt, question.StartedAt };
        await foreach (var row in answerQuery.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (!questionStats.TryGetValue(row.QuestionId, out var stats))
            {
                stats = new QuestionAggregate();
                questionStats.Add(row.QuestionId, stats);
            }
            stats.AnsweredCount++;
            if (!row.IsCorrect) stats.IncorrectCount++;
            if (row.StartedAt is not null && row.SubmittedAt >= row.StartedAt)
            {
                stats.TimedAnswerCount++;
                stats.TotalAnswerSeconds += (row.SubmittedAt - row.StartedAt.Value).TotalSeconds;
            }
        }

        var questionIds = questionStats.Keys.ToArray();
        var questionTexts = await db.Questions.AsNoTracking()
            .Where(question => questionIds.Contains(question.Id))
            .ToDictionaryAsync(question => question.Id, question => question.Text, cancellationToken);

        var slowestQuestions = questionStats.Select(pair => new QuizQuestionTimingDto(
            pair.Key, questionTexts.GetValueOrDefault(pair.Key, "Question unavailable"),
            pair.Value.TimedAnswerCount,
            pair.Value.TimedAnswerCount == 0 ? 0 : pair.Value.TotalAnswerSeconds / pair.Value.TimedAnswerCount))
            .Where(item => item.TimedAnswerCount > 0)
            .OrderByDescending(item => item.AverageAnswerSeconds).ThenBy(item => item.QuestionId)
            .Take(10).ToList();

        var mostMissedQuestions = questionStats.Select(pair => new QuizQuestionMistakesDto(
            pair.Key, questionTexts.GetValueOrDefault(pair.Key, "Question unavailable"),
            pair.Value.AnsweredCount, pair.Value.IncorrectCount))
            .Where(item => item.IncorrectCount > 0)
            .OrderByDescending(item => (double)item.IncorrectCount / item.AnsweredCount)
            .ThenByDescending(item => item.IncorrectCount)
            .ThenBy(item => item.QuestionId).Take(10).ToList();

        return new QuizDetailedReportDto(quiz.Id, quiz.Title, completedGames,
            validDurationCount == 0 ? null : totalDurationSeconds / validDurationCount,
            slowestQuestions, mostMissedQuestions);
    }

    private sealed class QuestionAggregate
    {
        public int AnsweredCount { get; set; }
        public int IncorrectCount { get; set; }
        public int TimedAnswerCount { get; set; }
        public double TotalAnswerSeconds { get; set; }
    }
}

public sealed record QuizStatisticsReportDto(Guid QuizId, string QuizTitle, int TotalCount,
    double AverageScore, int Page, int PageSize, int TotalPages, List<QuizAttemptDto> Items);

public sealed record QuizAttemptDto(Guid SessionId, string PlayerName, DateTime CompletedAt,
    int Score, int QuestionCount, int AnsweredCount, int CorrectCount);

public sealed record QuizAttemptDetailDto(Guid SessionId, string PlayerName, DateTime CompletedAt,
    int Score, List<PlayerStatisticsRowDto> Rows);

public sealed record QuizDetailedReportDto(Guid QuizId, string QuizTitle, int CompletedGames,
    double? AverageCompletionSeconds, List<QuizQuestionTimingDto> SlowestQuestions,
    List<QuizQuestionMistakesDto> MostMissedQuestions);

public sealed record QuizQuestionTimingDto(Guid QuestionId, string Question,
    int TimedAnswerCount, double AverageAnswerSeconds);

public sealed record QuizQuestionMistakesDto(Guid QuestionId, string Question,
    int AnsweredCount, int IncorrectCount);
