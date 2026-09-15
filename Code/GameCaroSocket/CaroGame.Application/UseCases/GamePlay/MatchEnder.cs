using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;

namespace CaroGame.Application.UseCases.GamePlay;

public sealed class EndMatchUseCase : IMatchEnder
{
    private readonly IRoomRepository _roomRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly TimeProvider _time;

    public EndMatchUseCase(
        IRoomRepository roomRepository,
        IPlayerRepository playerRepository, TimeProvider? time = null)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
        _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
        _time = time ?? TimeProvider.System;
    }

    public Room EndMatch(
        Guid roomId,
        MatchResultType matchResultType, string? reason = null)
    {


        if (!System.Enum.IsDefined(matchResultType))
            throw new ArgumentOutOfRangeException(
                nameof(matchResultType),
                matchResultType,
                "Unknown match result.");

        if (matchResultType == MatchResultType.Continue)
            throw new ArgumentException(
                "A match can only be ended with a final result.",
                nameof(matchResultType));
        var room = _roomRepository.GetById(roomId)
            ?? throw new KeyNotFoundException($"Room with ID '{roomId}' was not found.");
        if (room.Status == RoomStatus.Finished)
            return room;

        if (room.Status != RoomStatus.Playing || room.CurrentMatch is null)
            throw new InvalidOperationException("Room does not have an active match.");

        var playerX = _playerRepository.GetById(room.PlayerX.PlayerId);
        var playerO = _playerRepository.GetById(room.PlayerO.PlayerId);


        if (room.Status == RoomStatus.Finished)
            return room;

        room.EndMatch(matchResultType, _time.GetUtcNow().UtcDateTime, reason);

        ApplyResult(playerX, playerO, matchResultType);

        UpdatePlayer(playerX);
        UpdatePlayer(playerO);
        _roomRepository.Update(room);

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

    private void UpdatePlayer(Player? player)
    {
        if (player is null)
            return;

        if (player.Status != PlayerStatus.Offline)
            player.Status = PlayerStatus.Free;

        _playerRepository.Update(player);
    }
}
