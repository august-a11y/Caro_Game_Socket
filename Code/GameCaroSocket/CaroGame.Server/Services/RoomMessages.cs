using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;
using CaroGame.Shared.Protocol.Contracts;

namespace CaroGame.Server.Services;


public static class RoomMessages
{
    public static Guid[] Recipients(Room room) =>
        new[] { room.PlayerX.PlayerId, room.PlayerO.PlayerId }.Concat(room.Spectators).ToArray();

    public static Guid[] Players(Room room) =>
        [room.PlayerX.PlayerId, room.PlayerO.PlayerId];

    public static MoveDto Move(Move move) => new(
        move.MoveNumber, move.PlayerId, move.Position.Y, move.Position.X,
        move.Symbol.ToString(), move.Timestamp);

    public static RoomSnapshot Snapshot(Room room, DateTime now)
    {
        var match = room.CurrentMatch;
        var playing = room.Status == RoomStatus.Playing && match is not null;
        var paused = playing && match!.TurnManager.IsPaused;
        var board = match is null ? Array.Empty<string?[]>() :
            Enumerable.Range(0, match.Board.Size).Select(row =>
                Enumerable.Range(0, match.Board.Size).Select(column =>
                    match.Board.GetSymbol(new Position(column, row))?.ToString()).ToArray()).ToArray();
        return new RoomSnapshot(
            room.RoomId, room.PlayerX.PlayerId, room.PlayerO.PlayerId, room.Status.ToString(),
            match?.IsFinished == true ? match.Result.ToString() : null, board,
            playing ? match!.TurnManager.CurrentTurnPlayerId : null,
            playing && !paused ? match!.TurnManager.TurnDeadline : null,
            playing ? Math.Max(0, (int)Math.Ceiling(match!.TurnManager.GetTimeRemaining(now).TotalSeconds)) : 0,
            paused, room.ReadyPlayers.ToArray(), room.Spectators.Count,
            match?.MoveHistory.Select(Move).ToArray() ?? [],
            room.Disconnected.Values.Select(info =>
                new DisconnectedPlayerDto(info.PlayerId, info.GracePeriodEndsAt)).ToArray(),
            room.ReadyDeadline, room.ClosedAt, room.ClosingReason);
    }
}
