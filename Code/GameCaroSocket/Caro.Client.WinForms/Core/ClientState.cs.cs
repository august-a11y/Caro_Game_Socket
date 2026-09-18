using CaroGame.Shared.Protocol.Contracts;

namespace Caro.Client.WinForms.Core;

internal sealed record ClientState
{
    public bool IsJoined { get; init; }
    public Guid? PlayerId { get; init; }
    public Guid? SessionId { get; init; }
    public PlayerDto? CurrentPlayer { get; init; }
    public RoomSnapshot? Room { get; init; }
    public RoomSnapshot? LastRoom { get; init; }
    public DateTime? LastHeartbeatAt { get; init; }
    public DateTime? ServerTime { get; init; }
    public TimeSpan? RoundTripTime { get; init; }
}
