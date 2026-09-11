using Backend.Data;
using Backend.Features.GameRooms;
using Backend.Features.GameSessions;
using Backend.Features.Quizes;
using Backend.Features.Quizes.Dtos;
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
    public async Task CreateFromRoomAsync_AllQuestionsModeSelectsEveryQuizQuestion()
    {
        await using var dbContext = CreateDbContext();
        var (quiz, questions) = await SeedQuizAsync(dbContext, questionsPerGame: 1, questionCount: 3);
        quiz.QuestionCountMode = QuestionCountMode.AllQuestions;
        await dbContext.SaveChangesAsync();
        var room = CreateRoom(quiz.Id, questionCount: 1);
        room.QuestionCountMode = QuestionCountMode.AllQuestions;

        var session = await new GameSessionService(dbContext)
            .CreateFromRoomAsync(room, CancellationToken.None);

        Assert.Equal(questions.Count, session.Questions.Count);
        Assert.Equal(questions.Count, room.QuestionCount);
    }

    [Fact]
    public async Task CreateFromRoomAsync_OnlySelectsQuestionsLinkedToRequestedQuiz()
    {
        await using var dbContext = CreateDbContext();
        var (quiz, questions) = await SeedQuizAsync(dbContext, questionsPerGame: 1, questionCount: 1);
        var otherQuiz = new Quiz { ThemeId = quiz.ThemeId, Title = "Other quiz", QuestionsPerGame = 1 };
        var otherQuestion = CreateQuestion(quiz.ThemeId, 2);
        dbContext.AddRange(otherQuiz, otherQuestion);
        dbContext.QuizQuestions.Add(new QuizQuestion { QuizId = otherQuiz.Id, QuestionId = otherQuestion.Id });
        await dbContext.SaveChangesAsync();

        var session = await new GameSessionService(dbContext)
            .CreateFromRoomAsync(CreateRoom(quiz.Id, 1), CancellationToken.None);

        Assert.Equal(questions.Single().Id, session.Questions.Single().QuestionId);
        Assert.DoesNotContain(session.Questions, item => item.QuestionId == otherQuestion.Id);
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
    public async Task GetPlayerStatisticsAsync_ReturnsAnsweredAndSkippedQuestions()
    {
        await using var dbContext = CreateDbContext();
        var (quiz, questions) = await SeedQuizAsync(dbContext, questionsPerGame: 2, questionCount: 2);
        var room = CreateRoom(quiz.Id, questionCount: 2);
        var player = room.Players.Single();
        var service = new GameSessionService(dbContext);
        var session = await service.CreateFromRoomAsync(room, CancellationToken.None);
        room.CurrentQuestionIndex = 0;

        var answeredQuestion = questions.Single(question => question.Id == room.QuestionIds[0]);
        await service.SubmitAnswerAsync(
            room,
            player.PlayerId,
            answeredQuestion.CorrectOptionId,
            CancellationToken.None);

        var statistics = await service.GetPlayerStatisticsAsync(
            session.Id,
            player.PlayerId,
            CancellationToken.None);

        Assert.Equal(2, statistics.Rows.Count);
        var answered = statistics.Rows.Single(row => row.Question == answeredQuestion.Text);
        Assert.Equal("Correct", answered.PlayerAnswer);
        Assert.Equal("Correct", answered.CorrectAnswer);
        Assert.Null(answered.Explanation);
        var skipped = statistics.Rows.Single(row => row.Question != answeredQuestion.Text);
        Assert.Null(skipped.PlayerAnswer);
        Assert.Equal("Correct", skipped.CorrectAnswer);
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

    [Fact]
    public async Task CreateQuizAsync_PersistsRequestedGameMode()
    {
        await using var dbContext = CreateDbContext();
        var theme = new QuizTheme { Name = "Science" };
        dbContext.QuizThemes.Add(theme);
        await dbContext.SaveChangesAsync();

        var quiz = await new QuizCatalog(dbContext).CreateQuizAsync(
            "Study quiz",
            theme.Id,
            1,
            QuestionCountMode.HostSelectable,
            GameMode.Study,
            CancellationToken.None);

        Assert.Equal(GameMode.Study, quiz.GameMode);
    }

    [Fact]
    public async Task QuizDesigner_CreateQuizAsync_RejectsUnsupportedGameMode()
    {
        await using var dbContext = CreateDbContext();
        var theme = new QuizTheme { Name = "Science" };
        dbContext.QuizThemes.Add(theme);
        await dbContext.SaveChangesAsync();

        var request = new CreateQuizRequest
        {
            Title = "Invalid mode quiz",
            ThemeId = theme.Id,
            QuestionsPerGame = 1,
            GameMode = (GameMode)99,
        };

        await Assert.ThrowsAsync<ArgumentException>(() => new QuizDesigner(dbContext)
            .CreateQuizAsync(request, CancellationToken.None));
    }

    [Fact]
    public void CreateGameRoom_StudyModeDisablesAnswerTimer()
    {
        var service = new GameRoomService();

        var room = service.CreateGameRoom(
            Guid.NewGuid(),
            "Student",
            1,
            30,
            QuestionSelectionMode.AscendingDifficulty,
            null,
            QuestionCountMode.HostSelectable,
            GameMode.Study,
            isSolo: true);

        Assert.Equal(GameMode.Study, room.GameMode);
        Assert.Null(room.AnswerTimeLimitSeconds);

        var host = room.Players.Single();
        var reconnectedHost = service.JoinPlayer(
            room.GameCode,
            host.Name,
            host.PlayerToken,
            "host-reconnected");

        Assert.Same(host, reconnectedHost);

        var exception = Assert.Throws<InvalidOperationException>(() => service.JoinPlayer(
            room.GameCode,
            "Another student",
            null,
            "second-connection"));

        Assert.Equal("Study games can only be played solo.", exception.Message);
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
            Status = QuizStatus.Published,
        };
        var questions = Enumerable.Range(1, questionCount)
            .Select(index => CreateQuestion(theme.Id, index))
            .ToList();

        dbContext.AddRange(theme, quiz);
        dbContext.Questions.AddRange(questions);
        dbContext.QuizQuestions.AddRange(questions.Select(question => new QuizQuestion
        {
            QuizId = quiz.Id,
            QuestionId = question.Id,
        }));
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
