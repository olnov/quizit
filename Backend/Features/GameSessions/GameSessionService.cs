using Backend.Data;
using Backend.Features.GameRooms;
using Backend.Features.GameRooms.Dtos;
using Backend.Features.Quizes;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.GameSessions;

public class GameSessionService
{
    private readonly AppDbContext _dbContext;

    public GameSessionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GameSession> CreateFromRoomAsync(
        GameRoom room,
        CancellationToken cancellationToken)
    {
        var quiz = await _dbContext.Quizes
            .SingleOrDefaultAsync(current => current.Id == room.QuizId, cancellationToken)
            ?? throw new KeyNotFoundException($"Quiz with id '{room.QuizId}' was not found.");

        var questionIds = await _dbContext.Questions
            .Where(question => question.ThemeId == quiz.ThemeId)
            .Select(question => question.Id)
            .ToListAsync(cancellationToken);

        if (questionIds.Count < room.QuestionCount)
        {
            throw new InvalidOperationException("The quiz theme does not contain enough questions.");
        }

        var selectedQuestionIds = questionIds
            .OrderBy(_ => Random.Shared.Next())
            .Take(room.QuestionCount)
            .ToList();

        var session = new GameSession
        {
            GameRoomId = room.GameId,
            QuizId = room.QuizId,
            Players = room.Players
                .Select(player => new GameSessionPlayer
                {
                    PlayerId = player.PlayerId,
                    Name = player.Name,
                    Score = player.Score,
                })
                .ToList(),
            Questions = selectedQuestionIds
                .Select((questionId, order) => new GameSessionQuestion
                {
                    GameSessionId = Guid.Empty,
                    QuestionId = questionId,
                    Order = order,
                })
                .ToList(),
        };

        foreach (var question in session.Questions)
        {
            question.GameSessionId = session.Id;
        }

        foreach (var player in session.Players)
        {
            player.GameSessionId = session.Id;
        }

        _dbContext.GameSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
        room.GameSessionId = session.Id;
        room.QuestionIds = selectedQuestionIds;
        return session;
    }

    public async Task<GameSession?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.GameSessions
            .Include(session => session.Players)
            .Include(session => session.Questions)
            .Include(session => session.Answers)
            .SingleOrDefaultAsync(session => session.Id == id, cancellationToken);
    }

    public async Task<CurrentQuestionDto> GetCurrentQuestionAsync(
        GameRoom room,
        CancellationToken cancellationToken)
    {
        var questionId = GetCurrentQuestionId(room);
        var question = await _dbContext.Questions
            .Include(current => current.Options)
            .SingleOrDefaultAsync(current => current.Id == questionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Question with id '{questionId}' was not found.");

        return new CurrentQuestionDto
        {
            Index = room.CurrentQuestionIndex,
            AnswerDeadlineAt = room.AnswerDeadlineAt,
            Question = QuizMapper.ToDto(question),
        };
    }

    public async Task<SubmittedAnswer> SubmitAnswerAsync(
        GameRoom room,
        string playerId,
        Guid answerOptionId,
        CancellationToken cancellationToken)
    {
        if (room.GameSessionId is null)
        {
            throw new InvalidOperationException("The game session has not started.");
        }

        var questionId = GetCurrentQuestionId(room);
        var hasQuestion = await _dbContext.GameSessionQuestions.AnyAsync(question =>
            question.GameSessionId == room.GameSessionId && question.QuestionId == questionId,
            cancellationToken);
        if (!hasQuestion)
        {
            throw new InvalidOperationException("The current question is not part of this game session.");
        }

        var alreadyAnswered = await _dbContext.GameSessionAnswers.AnyAsync(answer =>
            answer.GameSessionId == room.GameSessionId
            && answer.PlayerId == playerId
            && answer.QuestionId == questionId,
            cancellationToken);
        if (alreadyAnswered)
        {
            throw new InvalidOperationException("The player has already answered this question.");
        }

        var question = await _dbContext.Questions
            .SingleOrDefaultAsync(current => current.Id == questionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Question with id '{questionId}' was not found.");
        var answerOption = await _dbContext.AnswerOptions
            .SingleOrDefaultAsync(option =>
                option.Id == answerOptionId && option.QuestionId == questionId,
                cancellationToken)
            ?? throw new ArgumentException("The answer option does not belong to the current question.");

        var isCorrect = question.CorrectOptionId == answerOption.Id;
        var scoreAwarded = isCorrect ? GetScore(question.Difficulty) : 0;
        var sessionPlayer = await _dbContext.GameSessionPlayers.SingleOrDefaultAsync(player =>
            player.GameSessionId == room.GameSessionId && player.PlayerId == playerId,
            cancellationToken)
            ?? throw new InvalidOperationException("The player is not part of this game session.");

        _dbContext.GameSessionAnswers.Add(new GameSessionAnswer
        {
            GameSessionId = room.GameSessionId.Value,
            PlayerId = playerId,
            QuestionId = questionId,
            AnswerOptionId = answerOptionId,
            IsCorrect = isCorrect,
            ScoreAwarded = scoreAwarded,
        });
        sessionPlayer.Score += scoreAwarded;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SubmittedAnswer
        {
            PlayerId = playerId,
            QuestionId = questionId,
            AnswerOptionId = answerOptionId,
            IsCorrect = isCorrect,
            ScoreAwarded = scoreAwarded,
        };
    }

    public async Task<RevealQuestionDto> GetRevealAsync(
        GameRoom room,
        CancellationToken cancellationToken)
    {
        var questionId = GetCurrentQuestionId(room);
        var question = await _dbContext.Questions
            .SingleOrDefaultAsync(current => current.Id == questionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Question with id '{questionId}' was not found.");

        return new RevealQuestionDto
        {
            QuestionId = questionId,
            CorrectOptionId = question.CorrectOptionId,
            Explanation = question.Explanation,
        };
    }

    public async Task CompleteAsync(GameSession gameSession, CancellationToken cancellationToken)
    {
        gameSession.Status = GameSessionStatus.Completed;
        gameSession.CompletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAsync(
        Guid gameSessionId,
        IReadOnlyCollection<PlayerState> players,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.GameSessions
            .Include(current => current.Players)
            .SingleOrDefaultAsync(current => current.Id == gameSessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Game session with id '{gameSessionId}' was not found.");

        foreach (var player in players)
        {
            var sessionPlayer = session.Players.Single(current => current.PlayerId == player.PlayerId);
            sessionPlayer.Score = player.Score;
        }

        session.Status = GameSessionStatus.Completed;
        session.CompletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(GameSession gameSession, CancellationToken cancellationToken)
    {
        gameSession.Status = GameSessionStatus.Cancelled;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Guid GetCurrentQuestionId(GameRoom room)
    {
        if (room.CurrentQuestionIndex < 0 || room.CurrentQuestionIndex >= room.QuestionIds.Count)
        {
            throw new InvalidOperationException("There is no active question.");
        }

        return room.QuestionIds[room.CurrentQuestionIndex];
    }

    private static int GetScore(QuestionDifficulty difficulty)
    {
        return difficulty switch
        {
            QuestionDifficulty.Easy => 100,
            QuestionDifficulty.Medium => 200,
            QuestionDifficulty.Hard => 300,
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty)),
        };
    }
}
