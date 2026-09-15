using CaroGame.Application.Contracts;

namespace CaroGame.Application.UseCases.Lobby
{
    public interface IOnlinePlayerFinder
    {
        List<PlayerInfo> FindOnlinePlayers();
    }
}

