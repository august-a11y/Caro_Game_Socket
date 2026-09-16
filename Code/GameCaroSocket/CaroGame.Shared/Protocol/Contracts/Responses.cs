namespace CaroGame.Shared.Protocol.Contracts;

public record ErrorResponse(Guid? RequestId, string ErrorCode, string Message);
public record PlayerDto(Guid PlayerId, string Nickname, string Status);
public record SessionResponse(Guid? RequestId, Guid PlayerId, Guid SessionId, PlayerDto Player, RoomSnapshot? Room, RoomSnapshot? LastRoom = null);
public record PlayersResponse(Guid? RequestId, IReadOnlyList<PlayerDto> Players);
public  record MatchSummaryDto(Guid RoomId, string PlayerXName, string PlayerOName, int SpectatorCount, string Status, DateTime StartedAt);
public  record MatchesResponse(Guid? RequestId, IReadOnlyList<MatchSummaryDto> Matches);
public  record HeartbeatResponse(Guid? RequestId, DateTime ServerTime);
public  record UdpEndpointResponse(Guid? RequestId, string Address, int Port);
public  record ChallengeDto(Guid ChallengeId, Guid FromPlayerId, Guid ToPlayerId, DateTime ExpiresAt, string Status);
public  record ChallengeResponse(Guid? RequestId, ChallengeDto Challenge, RoomSnapshot? Room = null);
public  record RoomResponse(Guid? RequestId, RoomSnapshot Room);
public  record RoomPlayerNotification(Guid? RequestId, Guid RoomId, Guid PlayerId, int SpectatorCount);
public record RematchOfferNotification(Guid? RequestId, Guid RoomId);
public record RematchResponseNotification(Guid? RequestId, Guid RoomId, Guid PlayerId, bool Accepted, int AcceptedCount);
public  record PlayerOfflineNotification(Guid PlayerId);
public  record MoveDto(int MoveNumber, Guid PlayerId, int Row, int Column, string Symbol, DateTime Timestamp);
public  record MoveNotification(Guid? RequestId, Guid RoomId, MoveDto Move, Guid CurrentTurnPlayerId, DateTime TurnDeadline);
public  record GameOverNotification(Guid? RequestId, RoomSnapshot Room, string Reason);
public record RoomCancelledNotification(Guid? RequestId, RoomSnapshot Room, string Reason);
public  record MatchPausedNotification(Guid RoomId, Guid PlayerId, DateTime GracePeriodEndsAt, RoomSnapshot Room);
public  record TurnTimeoutNotification(Guid RoomId, Guid PlayerId);
public  record DisconnectedPlayerDto(Guid PlayerId, DateTime GracePeriodEndsAt);

public  record RoomSnapshot(
    Guid RoomId,
    Guid PlayerXId,
    Guid PlayerOId,
    string Status,
    string? Result,
    string?[][] Board,
    Guid? CurrentTurnPlayerId,
    DateTime? TurnDeadline,
    int TimeRemainingSec,
    bool IsPaused,
    IReadOnlyList<Guid> ReadyPlayers,
    int SpectatorCount,
    IReadOnlyList<MoveDto> MoveHistory,
    IReadOnlyList<DisconnectedPlayerDto> DisconnectedPlayers,
    DateTime? ReadyDeadline = null,
    DateTime? ClosedAt = null,
    string? ClosingReason = null);
