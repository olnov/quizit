using Microsoft.AspNetCore.Mvc;
using Backend.Features.GameRooms.Dtos;
using Backend.Features.GameSessions;
using Backend.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Features.GameRooms;

[ApiController]
[Route("api/v1/game-rooms")]
public class GameRoomController : ControllerBase
{
    private readonly GameRoomService _gameRoomService;
    private readonly GameSessionService _gameSessionService;
    private readonly IHubContext<GameHub> _gameHubContext;

    public GameRoomController(
        GameRoomService gameRoomService,
        GameSessionService gameSessionService,
        IHubContext<GameHub> gameHubContext)
    {
        _gameRoomService = gameRoomService;
        _gameSessionService = gameSessionService;
        _gameHubContext = gameHubContext;
    }

    [HttpGet(Name = "GetRoom")]
    public IActionResult GetRoom(string gameCode)
    {
        var room = _gameRoomService.GetRoom(gameCode);
        if (room == null)
        {
            return NotFound();
        }
        return Ok(GameRoomMapper.ToDto(room));
    }

    [HttpPost(Name = "CreateRoom")]
    public IActionResult CreateRoom([FromBody] CreateRoomRequest request)
    {
        var room = _gameRoomService.CreateGameRoom(
            request.QuizId,
            request.HostName,
            request.QuestionCount,
            request.AnswerTimeLimitSeconds);
        var host = room.Players.Single(player => player.PlayerId == room.HostPlayerId);

        return Created($"/api/v1/game-rooms/{room.GameCode}", new CreateRoomResponseDto
        {
            Room = GameRoomMapper.ToDto(room),
            Credentials = GameRoomMapper.ToCredentials(host),
        });
    }

    [HttpPost("{gameCode}/start")]
    public async Task<IActionResult> StartGame(
        string gameCode,
        [FromBody] StartGameRequest request,
        CancellationToken cancellationToken)
    {
        var room = _gameRoomService.GetRoom(gameCode)
            ?? throw new KeyNotFoundException($"Game room with code '{gameCode}' was not found.");

        if (!_gameRoomService.IsHost(gameCode, request.PlayerToken))
        {
            return Forbid();
        }

        _gameRoomService.EnsureCanStartGame(gameCode, request.PlayerToken);

        await _gameSessionService.CreateFromRoomAsync(room, cancellationToken);
        _gameRoomService.StartGame(gameCode, request.PlayerToken);
        var startedRoom = _gameRoomService.BeginQuestion(gameCode, request.PlayerToken);
        var response = GameRoomMapper.ToDto(startedRoom);
        await _gameHubContext.Clients.Group(gameCode)
            .SendAsync("GameStarted", response, cancellationToken);
        var question = await _gameSessionService.GetCurrentQuestionAsync(startedRoom, cancellationToken);
        await _gameHubContext.Clients.Group(gameCode)
            .SendAsync("QuestionStarted", question, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{gameCode}/questions/start")]
    public async Task<IActionResult> BeginQuestion(
        string gameCode,
        [FromBody] NextQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var room = _gameRoomService.BeginQuestion(gameCode, request.PlayerToken);
        var question = await _gameSessionService.GetCurrentQuestionAsync(room, cancellationToken);
        await _gameHubContext.Clients.Group(gameCode)
            .SendAsync("QuestionStarted", question, cancellationToken);
        await _gameHubContext.Clients.Group(gameCode)
            .SendAsync("RoomUpdated", GameRoomMapper.ToDto(room), cancellationToken);

        return Ok(question);
    }

    [HttpGet("{gameCode}/questions/current")]
    public async Task<IActionResult> GetCurrentQuestion(
        string gameCode,
        CancellationToken cancellationToken)
    {
        var room = _gameRoomService.GetRoom(gameCode)
            ?? throw new KeyNotFoundException($"Game room with code '{gameCode}' was not found.");
        if (room.Status is not (GameStatus.QuestionActive or GameStatus.QuestionReveal))
        {
            throw new InvalidOperationException("There is no question to display at the current game stage.");
        }

        return Ok(await _gameSessionService.GetCurrentQuestionAsync(room, cancellationToken));
    }

    [HttpPost("{gameCode}/answers")]
    public async Task<IActionResult> SubmitAnswer(
        string gameCode,
        [FromBody] SubmitAnswerRequest request,
        CancellationToken cancellationToken)
    {
        var room = _gameRoomService.GetRoom(gameCode)
            ?? throw new KeyNotFoundException($"Game room with code '{gameCode}' was not found.");
        var player = _gameRoomService.GetPlayer(gameCode, request.PlayerToken);
        var answer = await _gameSessionService.SubmitAnswerAsync(
            room,
            player.PlayerId,
            request.AnswerOptionId,
            cancellationToken);
        _gameRoomService.SubmitAnswer(gameCode, answer);

        var questionRevealed = _gameRoomService.TryRevealWhenAllConnectedPlayersAnswered(room);
        await _gameHubContext.Clients.Group(gameCode)
            .SendAsync("RoomUpdated", GameRoomMapper.ToDto(room), cancellationToken);

        if (questionRevealed)
        {
            var reveal = await _gameSessionService.GetRevealAsync(room, cancellationToken);
            await _gameHubContext.Clients.Group(gameCode)
                .SendAsync("QuestionRevealed", reveal, cancellationToken);
        }

        return Ok(new AnswerAcceptedDto { QuestionId = answer.QuestionId });
    }

    [HttpPost("{gameCode}/questions/reveal")]
    public async Task<IActionResult> RevealQuestion(
        string gameCode,
        [FromBody] RevealQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var room = _gameRoomService.RevealQuestion(gameCode, request.PlayerToken);
        var reveal = await _gameSessionService.GetRevealAsync(room, cancellationToken);
        await _gameHubContext.Clients.Group(gameCode)
            .SendAsync("QuestionRevealed", reveal, cancellationToken);
        await _gameHubContext.Clients.Group(gameCode)
            .SendAsync("RoomUpdated", GameRoomMapper.ToDto(room), cancellationToken);

        return Ok(reveal);
    }

    [HttpPost("{gameCode}/scoreboard")]
    public async Task<IActionResult> ShowScoreboard(
        string gameCode,
        [FromBody] NextQuestionRequest request,
        CancellationToken cancellationToken)
    {
        _gameRoomService.ShowScoreboard(gameCode, request.PlayerToken);
        var scoreboard = _gameRoomService.GetScoreboard(gameCode);
        await _gameHubContext.Clients.Group(gameCode)
            .SendAsync("ScoreboardUpdated", scoreboard, cancellationToken);
        await _gameHubContext.Clients.Group(gameCode)
            .SendAsync("RoomUpdated", GameRoomMapper.ToDto(_gameRoomService.GetRoom(gameCode)!), cancellationToken);

        return Ok(scoreboard);
    }

    [HttpPost("{gameCode}/next")]
    public async Task<IActionResult> NextQuestion(
        string gameCode,
        [FromBody] NextQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var room = _gameRoomService.NextQuestion(gameCode, request.PlayerToken);
        if (room.Status == GameStatus.Completed)
        {
            await _gameSessionService.CompleteAsync(
                room.GameSessionId ?? throw new InvalidOperationException("The game session was not found."),
                room.Players,
                cancellationToken);

            var completed = new GameCompletedDto
            {
                GameCode = room.GameCode,
                Players = _gameRoomService.GetScoreboard(gameCode).Players,
            };
            await _gameHubContext.Clients.Group(gameCode)
                .SendAsync("GameCompleted", completed, cancellationToken);
            return Ok(completed);
        }

        var question = await _gameSessionService.GetCurrentQuestionAsync(room, cancellationToken);
        await _gameHubContext.Clients.Group(gameCode)
            .SendAsync("QuestionStarted", question, cancellationToken);
        await _gameHubContext.Clients.Group(gameCode)
            .SendAsync("RoomUpdated", GameRoomMapper.ToDto(room), cancellationToken);
        return Ok(question);
    }

    [HttpPost("{gameCode}/settings")]
    public async Task<IActionResult> UpdateSettings(
        string gameCode,
        [FromBody] UpdateRoomSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var room = _gameRoomService.UpdateSettings(
            gameCode,
            request.PlayerToken,
            request.QuestionCount,
            request.AnswerTimeLimitSeconds);
        var response = GameRoomMapper.ToDto(room);
        await _gameHubContext.Clients.Group(gameCode)
            .SendAsync("LobbyUpdated", response, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{gameCode}/complete")]
    public async Task<IActionResult> CompleteGame(
        string gameCode,
        [FromBody] StartGameRequest request,
        CancellationToken cancellationToken)
    {
        if (!_gameRoomService.IsHost(gameCode, request.PlayerToken))
        {
            return Forbid();
        }

        var room = _gameRoomService.GetRoom(gameCode)
            ?? throw new KeyNotFoundException($"Game room with code '{gameCode}' was not found.");
        _gameRoomService.CompleteGame(gameCode);
        await _gameSessionService.CompleteAsync(
            room.GameSessionId ?? throw new InvalidOperationException("The game session was not found."),
            room.Players,
            cancellationToken);

        var completed = new GameCompletedDto
        {
            GameCode = room.GameCode,
            Players = _gameRoomService.GetScoreboard(gameCode).Players,
        };
        await _gameHubContext.Clients.Group(gameCode)
            .SendAsync("GameCompleted", completed, cancellationToken);

        return Ok(completed);
    }

    [HttpPost("{gameCode}/restart")]
    public async Task<IActionResult> RestartRound(
        string gameCode,
        [FromBody] StartGameRequest request,
        CancellationToken cancellationToken)
    {
        var room = _gameRoomService.RestartRound(gameCode, request.PlayerToken);
        var response = GameRoomMapper.ToDto(room);
        await _gameHubContext.Clients.Group(gameCode)
            .SendAsync("LobbyUpdated", response, cancellationToken);
        return Ok(response);
    }
}
