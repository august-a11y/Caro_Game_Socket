using CaroGame.Application.Contracts;

namespace CaroGame.Application.UseCases.Lobby;

public interface IOngoingMatchFinder
{
    Task<List<RoomSummary>> FindOngoingMatchesAsync(CancellationToken cancellationToken);
}

