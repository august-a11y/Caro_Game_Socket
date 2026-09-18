using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.Contracts;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;

namespace CaroGame.Application.UseCases.GamePlay;

public sealed class EndMatchUseCase : IMatchEnder
{
    private readonly IRoomRepository _roomRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IMatchHistoryRepository? _matchHistoryRepository;
    private readonly TimeProvider _time;

    public EndMatchUseCase(
        IRoomRepository roomRepository,
        IPlayerRepository playerRepository,
        TimeProvider? time = null,
        IMatchHistoryRepository? matchHistoryRepository = null)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
        _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
        _time = time ?? TimeProvider.System;
        _matchHistoryRepository = matchHistoryRepository;
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

        SaveHistory(room, playerX, playerO);

        ApplyResult(playerX, playerO, matchResultType);

        UpdatePlayer(playerX);
        UpdatePlayer(playerO);
        _roomRepository.Update(room);

        return room;
    }

    private void SaveHistory(Room room, Player? playerX, Player? playerO)
    {
        if (_matchHistoryRepository is null || room.CurrentMatch is null || room.ClosedAt is not DateTime endedAt)
            return;

        var match = room.CurrentMatch;
        var winnerName = match.Result switch
        {
            MatchResultType.PlayerXWin => playerX?.Nickname,
            MatchResultType.PlayerOWin => playerO?.Nickname,
            _ => null
        };
        var moves = match.MoveHistory
            .Select(move => new MatchMoveHistoryEntry(
                move.MoveNumber,
                move.PlayerId,
                move.Position.X,
                move.Position.Y,
                move.Symbol.ToString(),
                move.Timestamp))
            .ToArray();

        _matchHistoryRepository.Add(new MatchHistoryEntry(
            room.RoomId,
            match.PlayerXId,
            match.PlayerOId,
            playerX?.Nickname ?? "Unknown",
            playerO?.Nickname ?? "Unknown",
            winnerName,
            match.StartedAt,
            endedAt,
            match.Result,
            room.ClosingReason ?? match.Result.ToString(),
            moves));
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
