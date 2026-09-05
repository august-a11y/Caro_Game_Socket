using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.GamePlay;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.SessionUseCase
{
    public class GracePeriodExpiryHandler : IGracePeriodExpiryHandler
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IMatchEnder _matchEnder;
        private readonly TimeProvider _timeProvider;

        public GracePeriodExpiryHandler(
            IRoomRepository roomRepository,
            IMatchEnder matchEnder,
            TimeProvider timeProvider)
        {
            _roomRepository = roomRepository
                ?? throw new ArgumentNullException(nameof(roomRepository));
            _matchEnder = matchEnder
                ?? throw new ArgumentNullException(nameof(matchEnder));
            _timeProvider = timeProvider
                ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async Task<Room> HandleAsync(Guid roomId, Guid playerId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var room = await _roomRepository.GetByIdAsync(roomId);
            cancellationToken.ThrowIfCancellationRequested();

            if (room is null)
                throw new KeyNotFoundException($"Room with ID '{roomId}' was not found.");

            if (room.Status != RoomStatus.Playing || room.CurrentMatch is null ||
                !room.Disconnected.TryGetValue(playerId, out var disconnectInfo))
                return room;

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (now < disconnectInfo.GracePeriodEndsAt)
                return room;

            var playerXExpired = IsExpired(room, room.PlayerX.PlayerId, now);
            var playerOExpired = IsExpired(room, room.PlayerO.PlayerId, now);

            var result = (playerXExpired, playerOExpired) switch
            {
                (true, true) => MatchResultType.Draw,
                (true, false) => MatchResultType.PlayerOWin,
                (false, true) => MatchResultType.PlayerXWin,
                _ => throw new InvalidOperationException(
                    "The requested grace period has not expired.")
            };

            cancellationToken.ThrowIfCancellationRequested();
            return await _matchEnder.EndMatchAsync(room, result, cancellationToken);
        }

        private static bool IsExpired(Room room, Guid playerId, DateTime now) =>
            room.Disconnected.TryGetValue(playerId, out var info) &&
            now >= info.GracePeriodEndsAt;
    }
}
