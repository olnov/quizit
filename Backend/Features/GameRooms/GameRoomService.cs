
using Backend.Features.GameRooms.Dtos;

namespace Backend.Features.GameRooms;

public class GameRoomService
{
    private readonly Dictionary<string, GameRoom> _rooms = new(StringComparer.OrdinalIgnoreCase);

    public GameRoom CreateGameRoom(Guid quizId, string hostName, string connectionId)
    {
        var hostPlayer = new PlayerState
        {
            Name = hostName,
            ConnectionId = connectionId,
        };

        var room = new GameRoom
        {
            QuizId = quizId,
            GameCode = GenerateGameCode(),
            HostPlayerId = hostPlayer.PlayerId,
            Players = new List<PlayerState> { hostPlayer },
        };

        _rooms[room.GameCode] = room;

        return room;
    }

    public GameRoom? GetRoom(string gameCode)
    {
        return _rooms.TryGetValue(gameCode, out var room) ? room : null;
    }

    public PlayerState AddPlayer(string gameCode, string playerName, string connectionId)
    {
        var room = GetRequiredRoom(gameCode);

        var existingPlayer = room.Players.FirstOrDefault(player => player.ConnectionId == connectionId);
        if (existingPlayer is not null)
        {
            existingPlayer.Name = playerName;
            existingPlayer.IsConnected = true;
            return existingPlayer;
        }

        var player = new PlayerState
        {
            Name = playerName,
            ConnectionId = connectionId,
        };

        room.Players.Add(player);

        return player;
    }

    public void MarkPlayerDisconnected(string gameCode, string connectionId)
    {
        var room = GetRequiredRoom(gameCode);
        var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId)
            ?? throw new InvalidOperationException($"Player with connection id '{connectionId}' was not found.");

        player.IsConnected = false;
    }

    public GameRoom? MarkPlayerDisconnected(string connectionId)
    {
        foreach (var room in _rooms.Values)
        {
            var player = room.Players.FirstOrDefault(current => current.ConnectionId == connectionId);
            if (player is null)
            {
                continue;
            }

            player.IsConnected = false;
            return room;
        }

        return null;
    }

    public GameRoom StartGame(string gameCode, string hostPlayerId)
    {
        var room = GetRequiredRoom(gameCode);

        if (room.HostPlayerId != hostPlayerId)
        {
            throw new InvalidOperationException("Only the host can start the game.");
        }

        if (room.Status != GameStatus.Waiting)
        {
            throw new InvalidOperationException("The game room has already started.");
        }

        room.Status = GameStatus.Countdown;
        room.StartedAt = DateTime.UtcNow;
        room.CurrentQuestionIndex = 0;

        return room;
    }

    public SubmittedAnswer SubmitAnswer(
        string gameCode,
        string playerId,
        Guid questionId,
        Guid answerOptionId
    )
    {
        var room = GetRequiredRoom(gameCode);
        var answer = new SubmittedAnswer
        {
            PlayerId = playerId,
            QuestionId = questionId,
            AnswerOptionId = answerOptionId,
        };

        room.CurrentAnswers[playerId] = answer;

        return answer;
    }

    public ScoreboardDto GetScoreboard(string gameCode)
    {
        var room = GetRequiredRoom(gameCode);

        return new ScoreboardDto
        {
            GameCode = room.GameCode,
            Players = room.Players
                .OrderByDescending(player => player.Score)
                .ToList(),
        };
    }

    public void CompleteGame(string gameCode)
    {
        var room = GetRequiredRoom(gameCode);
        room.Status = GameStatus.Completed;
        room.CompletedAt = DateTime.UtcNow;
    }

    private GameRoom GetRequiredRoom(string gameCode)
    {
        return GetRoom(gameCode)
            ?? throw new KeyNotFoundException($"Game room with code '{gameCode}' was not found.");
    }

    private static string GenerateGameCode()
    {
        return Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }
}
