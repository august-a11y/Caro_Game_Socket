using System.Threading;
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;

namespace CaroGame.Application.UseCases.GamePlay
{
    public sealed class EndMatchUseCase : IMatchEnder
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IPlayerRepository _playerRepository;

        public EndMatchUseCase(IRoomRepository roomRepository, IPlayerRepository playerRepository)
        {
            _roomRepository = roomRepository;
            _playerRepository = playerRepository;
        }

        public async Task<Room> EndMatchAsync(
            Guid roomId,
            MatchResultType matchResultType,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var room = await _roomRepository.GetByIdAsync(roomId)
                ?? throw new InvalidOperationException($"Room '{roomId}' was not found.");

            if (room.Status != RoomStatus.Finished)
            {
                room.Finish();

                await ApplyStatsAsync(room, matchResultType);

                await _roomRepository.UpdateAsync(room);
            }

            return room;
        }

        private async Task ApplyStatsAsync(Room room, MatchResultType result)
        {
            switch (result)
            {
                case MatchResultType.PlayerAWin:
                    await RecordWinLossAsync(room.PlayerA.PlayerId, room.PlayerB.PlayerId);
                    break;

                case MatchResultType.PlayerBWin:
                    await RecordWinLossAsync(room.PlayerB.PlayerId, room.PlayerA.PlayerId);
                    break;

                case MatchResultType.Draw:
                    await RecordDrawAsync(room.PlayerA.PlayerId);
                    await RecordDrawAsync(room.PlayerB.PlayerId);
                    break;

                case MatchResultType.Continue:
                    break;
            }
        }

        private async Task RecordWinLossAsync(Guid winnerId, Guid loserId)
        {
            var winner = await _playerRepository.GetByIdAsync(winnerId);
            if (winner is not null)
            {
                winner.Stats.RecordWin();
                await _playerRepository.UpdateAsync(winner);
            }

            var loser = await _playerRepository.GetByIdAsync(loserId);
            if (loser is not null)
            {
                loser.Stats.RecordLoss();
                await _playerRepository.UpdateAsync(loser);
            }
        }

        private async Task RecordDrawAsync(Guid playerId)
        {
            var player = await _playerRepository.GetByIdAsync(playerId);
            if (player is not null)
            {
                player.Stats.RecordDraw();
                await _playerRepository.UpdateAsync(player);
            }
        }
    }
}