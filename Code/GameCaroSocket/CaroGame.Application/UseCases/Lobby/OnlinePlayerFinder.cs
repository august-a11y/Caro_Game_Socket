using CaroGame.Application.Contracts;
using CaroGame.Application.Interfaces.Repositories;

namespace CaroGame.Application.UseCases.Lobby
{
    public sealed class OnlinePlayerFinder : IOnlinePlayerFinder
    {
        private readonly IPlayerRepository _playerRepository;

        public OnlinePlayerFinder(IPlayerRepository playerRepository)
        {
            _playerRepository = playerRepository;
        }

        public async Task<List<PlayerInfo>> FindOnlinePlayersAsync(
            CancellationToken cancellationToken)
        {
            var players = await _playerRepository.GetOnlinePlayersAsync();

            return players
                .Select(player => new PlayerInfo
                {
                    UserId = player.PlayerId,
                    Nickname = player.Nickname,
                    Status = CaroGame.Domain.Enum.PlayerStatus.Free
                })
                .ToList();
        }
    }
}