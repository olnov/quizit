
using System.Collections.Concurrent;
using Backend.Features.GameRooms.Dtos;

namespace Backend.Features.GameRooms;

public class GameRoomService
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new(StringComparer.OrdinalIgnoreCase);

    public GameRoom CreateGameRoom(
        Guid quizId,
        string hostName,
        int questionCount,
        int? answerTimeLimitSeconds)
    {
        var hostPlayer = new PlayerState
        {
            Name = hostName,
        };

        var room = new GameRoom
        {
            QuizId = quizId,
            GameCode = GenerateGameCode(),
            HostPlayerId = hostPlayer.PlayerId,
            Players = new List<PlayerState> { hostPlayer },
            QuestionCount = questionCount,
            AnswerTimeLimitSeconds = answerTimeLimitSeconds,
        };

        if (!_rooms.TryAdd(room.GameCode, room))
        {
            throw new InvalidOperationException("Unable to create a unique game room code.");
        }

        return room;
    }

    public GameRoom? GetRoom(string gameCode)
    {
        if (!_rooms.TryGetValue(gameCode, out var room))
        {
            return null;
        }

        if (IsExpiredWaitingRoom(room))
        {
            _rooms.TryRemove(gameCode, out _);
            return null;
        }

        return room;
    }

    public void CleanupExpiredRooms()
    {
        foreach (var (gameCode, room) in _rooms)
        {
            if (IsExpiredWaitingRoom(room))
            {
                _rooms.TryRemove(gameCode, out _);
            }
        }
    }

    public void EnsureCanStartGame(string gameCode, string playerToken)
    {
        var room = GetRequiredRoom(gameCode);
        EnsureHost(room, playerToken);

        if (room.Status != GameStatus.Waiting)
        {
            throw new InvalidOperationException("The game room has already started.");
        }

        if (!room.Players.Any(player => player.PlayerId != room.HostPlayerId && player.IsConnected))
        {
            throw new InvalidOperationException("At least one connected player is required to start the game.");
        }
    }

    public bool IsHost(string gameCode, string playerToken)
    {
        var room = GetRequiredRoom(gameCode);
        var player = room.Players.FirstOrDefault(current => current.PlayerToken == playerToken);
        return player?.PlayerId == room.HostPlayerId;
    }

    public PlayerState GetPlayer(string gameCode, string playerToken)
    {
        var room = GetRequiredRoom(gameCode);
        return room.Players.FirstOrDefault(player => player.PlayerToken == playerToken)
            ?? throw new InvalidOperationException("The player credentials are not valid for this game room.");
    }

    public PlayerState JoinPlayer(
        string gameCode,
        string playerName,
        string? playerToken,
        string connectionId)
    {
        var room = GetRequiredRoom(gameCode);

        var existingPlayer = room.Players.FirstOrDefault(player => player.PlayerToken == playerToken);
        if (existingPlayer is not null)
        {
            existingPlayer.Name = playerName;
            existingPlayer.ConnectionId = connectionId;
            existingPlayer.IsConnected = true;
            return existingPlayer;
        }

        if (room.Status != GameStatus.Waiting)
        {
            throw new InvalidOperationException("Players cannot join after the game has started.");
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

    public GameRoom StartGame(string gameCode, string playerToken)
    {
        var room = GetRequiredRoom(gameCode);
        EnsureCanStartGame(gameCode, playerToken);

        room.Status = GameStatus.Countdown;
        room.StartedAt = DateTime.UtcNow;
        room.CurrentQuestionIndex = 0;

        return room;
    }

    public GameRoom UpdateSettings(
        string gameCode,
        string playerToken,
        int questionCount,
        int? answerTimeLimitSeconds)
    {
        var room = GetRequiredRoom(gameCode);
        EnsureHost(room, playerToken);
        if (room.Status != GameStatus.Waiting)
        {
            throw new InvalidOperationException("Room settings can be changed only before the game starts.");
        }

        room.QuestionCount = questionCount;
        room.AnswerTimeLimitSeconds = answerTimeLimitSeconds;
        return room;
    }

    public SubmittedAnswer SubmitAnswer(
        string gameCode,
        SubmittedAnswer answer
    )
    {
        var room = GetRequiredRoom(gameCode);

        if (room.Status != GameStatus.QuestionActive)
        {
            throw new InvalidOperationException("Answers are not accepted at the current game stage.");
        }

        if (room.CurrentAnswers.ContainsKey(answer.PlayerId))
        {
            throw new InvalidOperationException("The player has already answered this question.");
        }

        var player = room.Players.SingleOrDefault(current => current.PlayerId == answer.PlayerId)
            ?? throw new InvalidOperationException("The player is not part of this game room.");

        player.Score += answer.ScoreAwarded;
        room.CurrentAnswers[answer.PlayerId] = answer;

        return answer;
    }

    public GameRoom BeginQuestion(string gameCode, string playerToken)
    {
        var room = GetRequiredRoom(gameCode);
        EnsureHost(room, playerToken);

        if (room.Status != GameStatus.Countdown)
        {
            throw new InvalidOperationException("The game is not ready to start a question.");
        }

        room.CurrentAnswers.Clear();
        room.Status = GameStatus.QuestionActive;
        room.AnswerDeadlineAt = room.AnswerTimeLimitSeconds is null
            ? null
            : DateTime.UtcNow.AddSeconds(room.AnswerTimeLimitSeconds.Value);
        return room;
    }

    public GameRoom RevealQuestion(string gameCode, string playerToken)
    {
        var room = GetRequiredRoom(gameCode);
        EnsureHost(room, playerToken);

        EnsureQuestionCanBeRevealed(room);
        RevealQuestion(room);
        return room;
    }

    public bool TryRevealWhenAllConnectedPlayersAnswered(GameRoom room)
    {
        if (room.Status != GameStatus.QuestionActive || !HaveAllConnectedPlayersAnswered(room))
        {
            return false;
        }

        RevealQuestion(room);
        return true;
    }

    public IReadOnlyCollection<GameRoom> GetTimedOutQuestions()
    {
        return _rooms.Values
            .Where(room => room.Status == GameStatus.QuestionActive
                && room.AnswerDeadlineAt is not null
                && room.AnswerDeadlineAt <= DateTime.UtcNow)
            .ToList();
    }

    public bool TryRevealTimedOutQuestion(GameRoom room)
    {
        if (room.Status != GameStatus.QuestionActive
            || room.AnswerDeadlineAt is null
            || room.AnswerDeadlineAt > DateTime.UtcNow)
        {
            return false;
        }

        RevealQuestion(room);
        return true;
    }

    public GameRoom ShowScoreboard(string gameCode, string playerToken)
    {
        var room = GetRequiredRoom(gameCode);
        EnsureHost(room, playerToken);

        if (room.Status != GameStatus.QuestionReveal)
        {
            throw new InvalidOperationException("Reveal the answer before showing the scoreboard.");
        }

        room.Status = GameStatus.Scoreboard;
        return room;
    }

    public GameRoom NextQuestion(string gameCode, string playerToken)
    {
        var room = GetRequiredRoom(gameCode);
        EnsureHost(room, playerToken);

        if (room.Status == GameStatus.QuestionReveal)
        {
            room.Status = GameStatus.Scoreboard;
        }

        if (room.Status != GameStatus.Scoreboard)
        {
            throw new InvalidOperationException("Reveal the answer before moving to the next question.");
        }

        if (room.CurrentQuestionIndex + 1 >= room.QuestionIds.Count)
        {
            CompleteGame(gameCode);
            return room;
        }

        room.CurrentQuestionIndex++;
        room.CurrentAnswers.Clear();
        room.Status = GameStatus.QuestionActive;
        return room;
    }

    public ScoreboardDto GetScoreboard(string gameCode)
    {
        var room = GetRequiredRoom(gameCode);

        return new ScoreboardDto
        {
            GameCode = room.GameCode,
            Players = room.Players
                .OrderByDescending(player => player.Score)
                .Select(player => new ScoreboardPlayerDto
                {
                    PlayerId = player.PlayerId,
                    Name = player.Name,
                    Score = player.Score,
                })
                .ToList(),
        };
    }

    public void CompleteGame(string gameCode)
    {
        var room = GetRequiredRoom(gameCode);
        room.Status = GameStatus.Completed;
        room.CompletedAt = DateTime.UtcNow;
        room.AnswerDeadlineAt = null;
    }

    public GameRoom RestartRound(string gameCode, string playerToken)
    {
        var room = GetRequiredRoom(gameCode);
        EnsureHost(room, playerToken);

        if (room.Status != GameStatus.Completed)
        {
            throw new InvalidOperationException("Only a completed game can be restarted.");
        }

        room.GameId = Guid.NewGuid().ToString();
        room.GameSessionId = null;
        room.Status = GameStatus.Waiting;
        room.QuestionIds.Clear();
        room.CurrentAnswers.Clear();
        room.CurrentQuestionIndex = -1;
        room.StartedAt = null;
        room.CompletedAt = null;
        room.AnswerDeadlineAt = null;
        room.LobbyExpiresAt = DateTime.UtcNow.AddMinutes(10);

        foreach (var player in room.Players)
        {
            player.Score = 0;
        }

        return room;
    }

    private GameRoom GetRequiredRoom(string gameCode)
    {
        return GetRoom(gameCode)
            ?? throw new KeyNotFoundException($"Game room with code '{gameCode}' was not found.");
    }

    private static void EnsureHost(GameRoom room, string playerToken)
    {
        var player = room.Players.FirstOrDefault(current => current.PlayerToken == playerToken);
        if (player is null || player.PlayerId != room.HostPlayerId)
        {
            throw new InvalidOperationException("Only the host can perform this action.");
        }
    }

    private static bool HaveAllConnectedPlayersAnswered(GameRoom room)
    {
        return room.Players
            .Where(player => player.IsConnected)
            .All(player => room.CurrentAnswers.ContainsKey(player.PlayerId));
    }

    private static void EnsureQuestionCanBeRevealed(GameRoom room)
    {
        if (room.Status != GameStatus.QuestionActive)
        {
            throw new InvalidOperationException("The current question is not active.");
        }

        if (!HaveAllConnectedPlayersAnswered(room)
            && (room.AnswerDeadlineAt is null || room.AnswerDeadlineAt > DateTime.UtcNow))
        {
            throw new InvalidOperationException("The question can be revealed only after all connected players answer or the timer expires.");
        }
    }

    private static void RevealQuestion(GameRoom room)
    {
        room.Status = GameStatus.QuestionReveal;
        room.AnswerDeadlineAt = null;
    }

    private static bool IsExpiredWaitingRoom(GameRoom room)
    {
        return room.Status == GameStatus.Waiting && room.LobbyExpiresAt <= DateTime.UtcNow;
    }

    private static string GenerateGameCode()
    {
        return Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }
}
