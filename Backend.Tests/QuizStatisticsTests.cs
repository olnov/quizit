using Backend.Data;
using Backend.Features.GameSessions;
using Backend.Features.GameRooms;
using Backend.Features.Quizes;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests;

public class QuizStatisticsTests
{
    [Fact]
    public async Task Report_FiltersCompletedSoloSessionsByQuizAndCompletionTime()
    {
        await using var db = CreateDb();
        var quizId = Guid.NewGuid();
        var otherQuizId = Guid.NewGuid();
        var from = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddDays(2);
        db.Quizes.AddRange(new Quiz { Id = quizId, Title = "Target" }, new Quiz { Id = otherQuizId, Title = "Other" });
        AddSession(db, quizId, from, "Start", 100);
        AddSession(db, quizId, to.AddTicks(-1), "End", 200);
        AddSession(db, quizId, to, "Outside", 300);
        AddSession(db, otherQuizId, from, "Other quiz", 400);
        AddSession(db, quizId, from, "Unfinished", 500, GameSessionStatus.InProgress);
        AddSession(db, quizId, from, "Competition", 600, extraPlayer: true);
        await db.SaveChangesAsync();

        var report = await new QuizStatisticsService(db, new GameSessionService(db))
            .GetReportAsync(quizId, from, to, 1, 20, CancellationToken.None);

        Assert.Equal(2, report.TotalCount);
        Assert.Equal(150, report.AverageScore);
        Assert.Equal(new[] { "End", "Start" }, report.Items.Select(item => item.PlayerName));
    }

    [Fact]
    public async Task Detail_IncludesUnansweredQuestionsAndRejectsOtherQuiz()
    {
        await using var db = CreateDb();
        var quizId = Guid.NewGuid();
        var otherQuizId = Guid.NewGuid();
        db.Quizes.AddRange(new Quiz { Id = quizId, Title = "Target" }, new Quiz { Id = otherQuizId, Title = "Other" });
        var session = AddSession(db, quizId, DateTime.UtcNow, "Player", 100);
        var question = new Question { Text = "Question", CorrectOptionId = Guid.NewGuid() };
        question.Options.Add(new Backend.Features.Quizes.AnswerOption { Id = question.CorrectOptionId, QuestionId = question.Id, Text = "Correct" });
        db.Questions.Add(question);
        db.GameSessionQuestions.Add(new GameSessionQuestion { GameSessionId = session.Id, QuestionId = question.Id, Order = 0 });
        await db.SaveChangesAsync();

        var service = new QuizStatisticsService(db, new GameSessionService(db));
        var detail = await service.GetDetailAsync(quizId, session.Id, CancellationToken.None);

        Assert.Single(detail.Rows);
        Assert.Null(detail.Rows[0].PlayerAnswer);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetDetailAsync(otherQuizId, session.Id, CancellationToken.None));
    }

    [Fact]
    public async Task DetailedReport_AveragesSessionDurationAndRanksTimedAndIncorrectQuestions()
    {
        await using var db = CreateDb();
        var quizId = Guid.NewGuid();
        var otherQuizId = Guid.NewGuid();
        var from = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddDays(1);
        db.Quizes.AddRange(new Quiz { Id = quizId, Title = "Target" }, new Quiz { Id = otherQuizId, Title = "Other" });
        var slow = new Question { Text = "Slow question" };
        var difficult = new Question { Text = "Difficult question" };
        db.Questions.AddRange(slow, difficult);

        var first = AddSession(db, quizId, from.AddMinutes(10), "First", 100);
        first.StartedAt = from;
        var second = AddSession(db, quizId, from.AddMinutes(40), "Second", 200);
        second.StartedAt = from.AddMinutes(20);
        var legacy = AddSession(db, quizId, from.AddMinutes(50), "Legacy", 0);
        legacy.StartedAt = from.AddMinutes(45);
        var excluded = AddSession(db, otherQuizId, from.AddMinutes(8), "Other", 0);
        excluded.StartedAt = from;

        AddAnswer(db, first, slow.Id, from.AddSeconds(90), from, true);
        AddAnswer(db, first, difficult.Id, from.AddMinutes(3), from.AddMinutes(2), false);
        AddAnswer(db, second, difficult.Id, from.AddMinutes(22), from.AddMinutes(21), false);
        AddAnswer(db, legacy, difficult.Id, from.AddMinutes(47), null, false);
        AddAnswer(db, excluded, slow.Id, from.AddMinutes(2), from, false);
        await db.SaveChangesAsync();

        var result = await new QuizStatisticsService(db, new GameSessionService(db))
            .GetDetailedReportAsync(quizId, from, to, CancellationToken.None);

        Assert.Equal(3, result.CompletedGames);
        Assert.Equal(700, result.AverageCompletionSeconds); // (600 + 1200 + 300) / 3
        Assert.Equal(new[] { slow.Id, difficult.Id }, result.SlowestQuestions.Select(q => q.QuestionId));
        Assert.Equal(90, result.SlowestQuestions[0].AverageAnswerSeconds);
        Assert.Equal(2, result.SlowestQuestions[1].TimedAnswerCount);
        Assert.Equal(difficult.Id, result.MostMissedQuestions[0].QuestionId);
        Assert.Equal(3, result.MostMissedQuestions[0].IncorrectCount);
        Assert.Equal(3, result.MostMissedQuestions[0].AnsweredCount);
    }

    [Fact]
    public async Task MarkQuestionStartedAsync_RecordsStartOnlyOnce()
    {
        await using var db = CreateDb();
        var session = new GameSession();
        var question = new GameSessionQuestion { GameSessionId = session.Id, QuestionId = Guid.NewGuid(), Order = 0 };
        db.GameSessions.Add(session);
        db.GameSessionQuestions.Add(question);
        await db.SaveChangesAsync();
        var room = new GameRoom { GameSessionId = session.Id, QuestionIds = [question.QuestionId], CurrentQuestionIndex = 0 };
        var service = new GameSessionService(db);

        await service.MarkQuestionStartedAsync(room, CancellationToken.None);
        var startedAt = question.StartedAt;
        await service.MarkQuestionStartedAsync(room, CancellationToken.None);

        Assert.NotNull(startedAt);
        Assert.Equal(startedAt, question.StartedAt);
    }

    [Fact]
    public void QuestionTimingMigration_IsDiscoverableAndMatchesTheModel()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=quizit_migration_check;Username=unused;Password=unused")
            .Options);

        Assert.Contains("20261009121500_AddGameSessionQuestionStartedAt", db.Database.GetMigrations());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    private static void AddAnswer(AppDbContext db, GameSession session, Guid questionId,
        DateTime submittedAt, DateTime? startedAt, bool isCorrect)
    {
        session.Questions.Add(new GameSessionQuestion
        {
            GameSessionId = session.Id, QuestionId = questionId,
            Order = session.Questions.Count,
            StartedAt = startedAt,
        });
        session.Answers.Add(new GameSessionAnswer
        {
            GameSessionId = session.Id, PlayerId = session.Players[0].PlayerId,
            QuestionId = questionId, SubmittedAt = submittedAt, IsCorrect = isCorrect,
        });
    }

    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static GameSession AddSession(AppDbContext db, Guid quizId, DateTime completedAt,
        string name, int score, GameSessionStatus status = GameSessionStatus.Completed, bool extraPlayer = false)
    {
        var session = new GameSession { QuizId = quizId, Status = status, CompletedAt = completedAt };
        session.Players.Add(new GameSessionPlayer { GameSessionId = session.Id, Name = name, PlayerId = Guid.NewGuid().ToString(), Score = score });
        if (extraPlayer)
            session.Players.Add(new GameSessionPlayer { GameSessionId = session.Id, Name = "Guest", PlayerId = Guid.NewGuid().ToString() });
        db.GameSessions.Add(session);
        return session;
    }
}
