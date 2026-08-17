using CaroGame.Application.Contracts;

namespace CaroGame.Application.UseCases.Lobby
{
    public interface IOngoingMatchFinder
    {
        Task<List<RoomSummary>> FindOngoingMatchAsync(Guid roomId, CancellationToken cancellationToken);
    }
}

