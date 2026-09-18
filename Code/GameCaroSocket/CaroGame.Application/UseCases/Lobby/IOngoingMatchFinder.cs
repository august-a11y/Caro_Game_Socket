using CaroGame.Application.Contracts;

namespace CaroGame.Application.UseCases.Lobby;

public interface IOngoingMatchFinder
{
    List<RoomSummary> FindOngoingMatches(Guid? roomId = null);
}

