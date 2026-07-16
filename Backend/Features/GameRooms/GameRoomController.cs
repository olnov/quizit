using Microsoft.AspNetCore.Mvc;
using Backend.Features.GameRooms.Dtos;

namespace Backend.Features.GameRooms;

[ApiController]
[Route("api/v1/game-rooms")]
public class GameRoomController : ControllerBase
{
    private readonly GameRoomService _gameRoomService;

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
}
