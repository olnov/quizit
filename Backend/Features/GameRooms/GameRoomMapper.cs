using Backend.Features.GameRooms.Dtos;

namespace Backend.Features.GameRooms;

public static class GameRoomMapper
{
    public static GameRoomDto ToDto(GameRoom room)
    {
        return new GameRoomDto
        {
            GameCode = room.GameCode,
            QuizId = room.QuizId,
            Status = room.Status,
            LobbyExpiresAt = room.LobbyExpiresAt,
            CurrentQuestionIndex = room.CurrentQuestionIndex,
            Players = room.Players.Select(player => new RoomPlayerDto
            {
                PlayerId = player.PlayerId,
                Name = player.Name,
                Score = player.Score,
                IsConnected = player.IsConnected,
            }).ToList(),
        };
    }

    public static PlayerCredentialsDto ToCredentials(PlayerState player)
    {
        return new PlayerCredentialsDto
        {
            PlayerId = player.PlayerId,
            PlayerToken = player.PlayerToken,
        };
    }
}
