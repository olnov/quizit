using Backend.Data;
using Backend.Features.GameRooms;
using Backend.Features.GameSessions;
using Backend.Features.Quizes;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests;

public class GameRulesTests
{
    [Fact]
    public async Task CreateQuestionAsync_RejectsAnythingOtherThanFourOptions()
    {
        await using var dbContext = CreateDbContext();
        var theme = new QuizTheme { Name = "Science" };
        dbContext.QuizThemes.Add(theme);
        await dbContext.SaveChangesAsync();
        var catalog = new QuizCatalog(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => catalog.CreateQuestionAsync(
            theme.Id,
            "How many planets are in the Solar System?",
            null,
            null,
            100,
            ["7", "8", "9"],
            1,
            CancellationToken.None));
    }

    [Fact]
    public async Task CreateQuestionAsync_RejectsDifficultyThatIsNotAMultipleOfOneHundred()
    {
        await using var dbContext = CreateDbContext();
        var theme = new QuizTheme { Name = "Science" };
        dbContext.QuizThemes.Add(theme);
        await dbContext.SaveChangesAsync();
        var catalog = new QuizCatalog(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => catalog.CreateQuestionAsync(
            theme.Id,
            "How many planets are in the Solar System?",
            null,
            null,
            101,
            ["7", "8", "9", "10"],
            1,
            CancellationToken.None));
    }

    [Fact]
    public async Task CreateFromRoomAsync_SelectsRequestedNumberOfUniqueQuestions()
    {
        await using var dbContext = CreateDbContext();
        var (quiz, questions) = await SeedQuizAsync(dbContext, questionsPerGame: 2, questionCount: 3);
        var room = CreateRoom(quiz.Id, questionCount: 2);
        var service = new GameSessionService(dbContext);

        var session = await service.CreateFromRoomAsync(room, CancellationToken.None);

        Assert.Equal(2, session.Questions.Count);
        Assert.Equal(2, session.Questions.Select(question => question.QuestionId).Distinct().Count());
        Assert.All(session.Questions, question => Assert.Contains(question.QuestionId, questions.Select(item => item.Id)));
        Assert.Equal(session.Id, room.GameSessionId);
    }

    [Fact]
    public async Task SubmitAnswerAsync_AwardsPointsForCorrectAnswer()
    {
        await using var dbContext = CreateDbContext();
        var (quiz, questions) = await SeedQuizAsync(dbContext, questionsPerGame: 1, questionCount: 1);
        var room = CreateRoom(quiz.Id, questionCount: 1);
        var player = room.Players.Single();
        var service = new GameSessionService(dbContext);
        await service.CreateFromRoomAsync(room, CancellationToken.None);
        room.CurrentQuestionIndex = 0;

        var question = questions.Single();
        var answer = await service.SubmitAnswerAsync(
            room,
            player.PlayerId,
            question.CorrectOptionId,
            CancellationToken.None);

        Assert.True(answer.IsCorrect);
        Assert.Equal(200, answer.ScoreAwarded);
        var sessionPlayer = await dbContext.GameSessionPlayers.SingleAsync();
        Assert.Equal(200, sessionPlayer.Score);
    }

    [Fact]
    public void GameRoomService_EnforcesStatusTransitions()
    {
        var service = new GameRoomService();
        var room = service.CreateGameRoom(
            Guid.NewGuid(),
            "Host",
            1,
            null,
            QuestionSelectionMode.AscendingDifficulty,
            null);
        room.QuestionIds = [Guid.NewGuid()];
        var hostToken = room.Players.Single().PlayerToken;
        var guest = new PlayerState { Name = "Guest" };
        room.Players.Add(guest);

        service.StartGame(room.GameCode, hostToken);
        Assert.Equal(GameStatus.Countdown, room.Status);

        service.BeginQuestion(room.GameCode, hostToken);
        Assert.Equal(GameStatus.QuestionActive, room.Status);

        room.CurrentAnswers[room.HostPlayerId] = new SubmittedAnswer { PlayerId = room.HostPlayerId };
        room.CurrentAnswers[guest.PlayerId] = new SubmittedAnswer { PlayerId = guest.PlayerId };

        service.RevealQuestion(room.GameCode, hostToken);
        Assert.Equal(GameStatus.QuestionReveal, room.Status);

        service.NextQuestion(room.GameCode, hostToken);
        Assert.Equal(GameStatus.Completed, room.Status);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(Quiz Quiz, List<Question> Questions)> SeedQuizAsync(
        AppDbContext dbContext,
        int questionsPerGame,
        int questionCount)
    {
        var theme = new QuizTheme { Name = "Science" };
        var quiz = new Quiz
        {
            ThemeId = theme.Id,
            Title = "Science Quiz",
            QuestionsPerGame = questionsPerGame,
        };
        var questions = Enumerable.Range(1, questionCount)
            .Select(index => CreateQuestion(theme.Id, index))
            .ToList();

        dbContext.AddRange(theme, quiz);
        dbContext.Questions.AddRange(questions);
        await dbContext.SaveChangesAsync();
        return (quiz, questions);
    }

    private static Question CreateQuestion(Guid themeId, int index)
    {
        var questionId = Guid.NewGuid();
        var correctOption = new AnswerOption
        {
            QuestionId = questionId,
            Text = "Correct",
        };
        var options = new List<AnswerOption>
        {
            correctOption,
            new() { QuestionId = questionId, Text = "Wrong 1" },
            new() { QuestionId = questionId, Text = "Wrong 2" },
            new() { QuestionId = questionId, Text = "Wrong 3" },
        };

        return new Question
        {
            Id = questionId,
            ThemeId = themeId,
            Text = $"Question {index}",
            Difficulty = 200,
            Options = options,
            CorrectOptionId = correctOption.Id,
        };
    }

    private static GameRoom CreateRoom(Guid quizId, int questionCount)
    {
        var player = new PlayerState { Name = "Player" };
        return new GameRoom
        {
            QuizId = quizId,
            QuestionCount = questionCount,
            HostPlayerId = player.PlayerId,
            Players = [player],
        };
    }
}
