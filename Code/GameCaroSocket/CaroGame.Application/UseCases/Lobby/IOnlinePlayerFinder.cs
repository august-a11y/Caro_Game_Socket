using CaroGame.Application.Contracts;

namespace CaroGame.Application.UseCases.Lobby
{
    public interface IOnlinePlayerFinder
    {
        Task<List<PlayerInfo>> FindOnlinePlayersAsync(CancellationToken cancellationToken);
    }
}

