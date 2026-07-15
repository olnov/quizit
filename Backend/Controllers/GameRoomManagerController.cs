using Microsoft.AspNetCore.Mvc;
using Quizz.Backend.Features.GameRooms;

namespace Quizz.Backend.Controllers;

[ApiController]
[Route("api/game-rooms")]
public class GameRoomManagerController : ControllerBase
{
    private readonly GameRoomManager _gameRoomManager;

    [HttpGet(Name = "GetRoom")]
    public IActionResult GetRoom(string gameCode)
    {
        var room = _gameRoomManager.GetRoom(gameCode);
        if (room == null)
        {
            return NotFound();
        }
        return Ok(room);
    }

    [HttpPost(Name = "CreateRoom")]
    public IActionResult CreateRoom([FromBody] CreateRoomRequest request)
    {
        var room = _gameRoomManager.CreateGameRoom(request.QuizId, request.HostName, request.ConnectionId);
        return Ok(room);
    }
}
