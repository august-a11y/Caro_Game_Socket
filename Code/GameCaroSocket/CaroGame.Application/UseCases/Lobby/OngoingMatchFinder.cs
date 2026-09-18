using CaroGame.Application.Contracts;
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Enum;

namespace CaroGame.Application.UseCases.Lobby;

public sealed class OngoingMatchFinder : IOngoingMatchFinder
{
    private readonly IRoomRepository _roomRepository;
    private readonly IPlayerRepository _playerRepository;

    public OngoingMatchFinder(
        IRoomRepository roomRepository,
        IPlayerRepository playerRepository)
    {
        ArgumentNullException.ThrowIfNull(roomRepository);
        ArgumentNullException.ThrowIfNull(playerRepository);
        _roomRepository = roomRepository;
        _playerRepository = playerRepository;
    }

    public List<RoomSummary> FindOngoingMatches(Guid? roomId = null)
    {
        IReadOnlyList<CaroGame.Domain.Entities.Room> rooms = roomId is Guid id
            ? _roomRepository.GetById(id) is { } selected ? new[] { selected } : []
            : _roomRepository.GetOngoingRooms();

        var result = new List<RoomSummary>();

        foreach (var room in rooms.Where(room => room.Status == RoomStatus.Playing))
        {
            var match = room.CurrentMatch ?? throw new InvalidOperationException(
                $"Playing room '{room.RoomId}' does not have a current match.");

            var playerX = _playerRepository.GetById(room.PlayerX.PlayerId)
                ?? throw new KeyNotFoundException(
                    $"Player X with ID '{room.PlayerX.PlayerId}' was not found for room '{room.RoomId}'.");

            var playerO = _playerRepository.GetById(room.PlayerO.PlayerId)
                ?? throw new KeyNotFoundException(
                    $"Player O with ID '{room.PlayerO.PlayerId}' was not found for room '{room.RoomId}'.");

            result.Add(new RoomSummary
            {
                RoomId = room.RoomId,
                PlayerAName = playerX.Nickname,
                PlayerBName = playerO.Nickname,
                SpectatorCount = room.Spectators.Count,
                Status = room.Status,
                StartedAt = match.StartedAt
            });
        }

        return result;
    }
}
