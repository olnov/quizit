using Microsoft.AspNetCore.Mvc;
using Backend.Features.GameRooms.Dtos;
using Backend.Features.GameSessions;

namespace Backend.Features.GameRooms;

[ApiController]
[Route("api/v1/game-rooms")]
public class GameRoomController : ControllerBase
{
    private readonly GameRoomService _gameRoomService;
    private readonly GameSessionService _gameSessionService;

    public GameRoomController(
        GameRoomService gameRoomService,
        GameSessionService gameSessionService)
    {
        _gameRoomService = gameRoomService;
        _gameSessionService = gameSessionService;
    }

    [HttpGet(Name = "GetRoom")]
    public IActionResult GetRoom(string gameCode)
    {
        var room = _gameRoomService.GetRoom(gameCode);
        if (room == null)
        {
            return NotFound();
        }
        return Ok(room);
    }

    [HttpPost(Name = "CreateRoom")]
    public IActionResult CreateRoom([FromBody] CreateRoomRequest request)
    {
        var room = _gameRoomService.CreateGameRoom(request.QuizId, request.HostName, request.ConnectionId);
        return Ok(room);
    }

    [HttpPost("{gameCode}/start")]
    public async Task<IActionResult> StartGame(
        string gameCode,
        [FromBody] StartGameRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var room = _gameRoomService.GetRoom(gameCode)
                ?? throw new KeyNotFoundException();

            if (room.HostPlayerId != request.HostPlayerId)
            {
                return Forbid();
            }

            if (room.Status != GameStatus.Waiting)
            {
                return Conflict(new { error = "The game room has already started." });
            }

            await _gameSessionService.CreateFromRoomAsync(room, cancellationToken);
            return Ok(_gameRoomService.StartGame(gameCode, request.HostPlayerId));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
