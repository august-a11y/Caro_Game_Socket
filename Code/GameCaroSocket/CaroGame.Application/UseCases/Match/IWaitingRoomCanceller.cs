using CaroGame.Domain.Entities;

namespace CaroGame.Application.UseCases.Match;

public interface IWaitingRoomCanceller
{
    Room Cancel(Guid roomId, string reason = "PlayerLeft");
}
