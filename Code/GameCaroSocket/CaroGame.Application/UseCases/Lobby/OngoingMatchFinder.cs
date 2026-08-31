using CaroGame.Application.Contracts;
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;

namespace CaroGame.Application.UseCases.Lobby
{
    public sealed class OngoingMatchFinder : IOngoingMatchFinder
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IPlayerRepository _playerRepository;

        public OngoingMatchFinder(
            IRoomRepository roomRepository,
            IPlayerRepository playerRepository)
        {
            _roomRepository = roomRepository;
            _playerRepository = playerRepository;
        }

        public async Task<List<RoomSummary>> FindOngoingMatchAsync(
            Guid roomId,
            CancellationToken cancellationToken)
        {
            var rooms = await _roomRepository.GetOngoingRoomsAsync();

            var result = new List<RoomSummary>();

            foreach (var room in rooms)
            {
                var playerX = await _playerRepository.GetByIdAsync(room.PlayerX.PlayerId);
                var playerO = await _playerRepository.GetByIdAsync(room.PlayerO.PlayerId);

                var summary = new RoomSummary
                {
                    RoomId = room.RoomId,
                    PlayerAName = playerX?.Nickname ?? string.Empty,
                    PlayerBName = playerO?.Nickname ?? string.Empty,
                    SpectatorCount = room.Spectators.Count,
                    Status = room.Status,
                    StartedAt = room.CreatedAt
                };

                result.Add(summary);
            }

            return result;
        }
    }
}