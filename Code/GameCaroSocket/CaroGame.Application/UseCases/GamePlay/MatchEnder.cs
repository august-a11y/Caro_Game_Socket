using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;

namespace CaroGame.Application.UseCases.GamePlay;

public sealed class EndMatchUseCase : IMatchEnder
{
    private readonly IRoomRepository _roomRepository;
    private readonly IPlayerRepository _playerRepository;

    public EndMatchUseCase(
        IRoomRepository roomRepository,
        IPlayerRepository playerRepository)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
        _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
    }

    public async Task<Room> EndMatchAsync(
        Room room,
        MatchResultType matchResultType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(room);
        cancellationToken.ThrowIfCancellationRequested();

        if (!System.Enum.IsDefined(matchResultType))
            throw new ArgumentOutOfRangeException(
                nameof(matchResultType),
                matchResultType,
                "Unknown match result.");

        if (matchResultType == MatchResultType.Continue)
            throw new ArgumentException(
                "A match can only be ended with a final result.",
                nameof(matchResultType));

        if (room.Status == RoomStatus.Finished)
            return room;

        if (room.Status != RoomStatus.Playing || room.CurrentMatch is null)
            throw new InvalidOperationException("Room does not have an active match.");

        var playerX = await _playerRepository.GetByIdAsync(room.PlayerX.PlayerId);
        var playerO = await _playerRepository.GetByIdAsync(room.PlayerO.PlayerId);

        cancellationToken.ThrowIfCancellationRequested();

        if (room.Status == RoomStatus.Finished)
            return room;

        room.EndMatch(matchResultType);

        ApplyResult(playerX, playerO, matchResultType);

        await UpdatePlayerAsync(playerX);
        await UpdatePlayerAsync(playerO);
        await _roomRepository.UpdateAsync(room);

        return room;
    }

    private static void ApplyResult(
        Player? playerX,
        Player? playerO,
        MatchResultType result)
    {
        switch (result)
        {
            case MatchResultType.PlayerXWin:
                playerX?.Stats.RecordWin();
                playerO?.Stats.RecordLoss();
                break;
            case MatchResultType.PlayerOWin:
                playerO?.Stats.RecordWin();
                playerX?.Stats.RecordLoss();
                break;
            case MatchResultType.Draw:
                playerX?.Stats.RecordDraw();
                playerO?.Stats.RecordDraw();
                break;
        }
    }

    private async Task UpdatePlayerAsync(Player? player)
    {
        if (player is null)
            return;

        if (player.Status != PlayerStatus.Offline)
            player.Status = PlayerStatus.Free;

        await _playerRepository.UpdateAsync(player);
    }
}
