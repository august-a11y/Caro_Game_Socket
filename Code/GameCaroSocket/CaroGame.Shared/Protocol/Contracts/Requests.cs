namespace CaroGame.Shared.Protocol.Contracts;

public record RequestMessage
{
    public Guid? RequestId { get; init; }
}

public sealed record PlayerJoinRequest : RequestMessage
{
    public required string Nickname { get; init; }
}

public sealed record PlayerReconnectRequest : RequestMessage
{
    public required Guid PlayerId { get; init; }
    public required Guid SessionId { get; init; }
}

public sealed record RegisterUdpEndpointRequest : RequestMessage
{
    public required int Port { get; init; }
}

public sealed record ChallengeRequest : RequestMessage
{
    public required Guid OpponentId { get; init; }
}

public record ChallengeReferenceRequest : RequestMessage
{
    public required Guid ChallengeId { get; init; }
}

public sealed record ChallengeRespondRequest : ChallengeReferenceRequest
{
    public required bool Accept { get; init; }
}

public record RoomRequest : RequestMessage
{
    public required Guid RoomId { get; init; }
}

public sealed record RematchResponseRequest : RoomRequest
{
    public required bool Accept { get; init; }
}

public sealed record SubmitMoveRequest : RoomRequest
{
    public required int Row { get; init; }
    public required int Column { get; init; }
}
