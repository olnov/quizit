using Backend.Data;
using Backend.Features.GameRooms;
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

        if (questionIds.Count < quiz.QuestionsPerGame)
        {
            throw new InvalidOperationException("The quiz theme does not contain enough questions.");
        }

        var selectedQuestionIds = questionIds
            .OrderBy(_ => Random.Shared.Next())
            .Take(quiz.QuestionsPerGame)
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
            .SingleOrDefaultAsync(session => session.Id == id, cancellationToken);
    }

    public async Task CompleteAsync(GameSession gameSession, CancellationToken cancellationToken)
    {
        gameSession.Status = GameSessionStatus.Completed;
        gameSession.CompletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(GameSession gameSession, CancellationToken cancellationToken)
    {
        gameSession.Status = GameSessionStatus.Cancelled;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
