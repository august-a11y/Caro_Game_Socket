using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.Interfaces.Repositories
{
    public interface IPlayerRepository
    {
        Task<Player?> GetByIdAsync(Guid playerId);

        Task<Player?> GetByNicknameAsync(string nickname);

        Task<IReadOnlyList<Player>> GetOnlinePlayersAsync();

        Task AddAsync(Player player);

        Task UpdateAsync(Player player);

        Task<bool> ExistsByNicknameAsync(string nickname);
    }
}
