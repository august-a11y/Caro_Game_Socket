using CaroGame.Domain.Enum;

namespace CaroGame.Application.Contracts;

public sealed record MatchHistoryEntry(
    Guid RoomId,
    Guid PlayerXId,
    Guid PlayerOId,
    string PlayerXName,
    string PlayerOName,
    string? WinnerName,
    DateTime StartedAt,
    DateTime EndedAt,
    MatchResultType Result,
    string Reason,
    IReadOnlyList<MatchMoveHistoryEntry> Moves);

public sealed record MatchMoveHistoryEntry(
    int MoveNumber,
    Guid PlayerId,
    int Row,
    int Column,
    string Symbol,
    DateTime Timestamp);
