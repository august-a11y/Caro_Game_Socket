using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;

namespace CaroGame.Application.UseCases.Match;

// Caller holds the lobby lock and this room's lock for the whole transition.
public sealed class WaitingRoomCanceller(IRoomRepository rooms, IPlayerRepository players, TimeProvider time) : IWaitingRoomCanceller
{
    public Room Cancel(Guid roomId, string reason = "PlayerLeft")
    {
        if (roomId == Guid.Empty) throw new ArgumentException("RoomId must not be empty.", nameof(roomId));
        var room = rooms.GetById(roomId) ?? throw new KeyNotFoundException("Room was not found.");
        if (room.Status == RoomStatus.Cancelled) return room;
        room.CancelWaiting(time.GetUtcNow().UtcDateTime, reason);
        foreach (var playerId in new[] { room.PlayerX.PlayerId, room.PlayerO.PlayerId })
        {
            var player = players.GetById(playerId) ?? throw new KeyNotFoundException("Player was not found.");
            if (player.Status != PlayerStatus.Offline) player.Status = PlayerStatus.Free;
            players.Update(player);
        }
        rooms.Update(room);
        return room;
    }
}
