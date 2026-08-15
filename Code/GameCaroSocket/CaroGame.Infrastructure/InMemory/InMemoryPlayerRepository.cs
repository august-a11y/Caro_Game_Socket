using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Infrastructure.InMemory
{
    public class InMemoryPlayerRepository : IPlayerRepository
    {
        public Task AddAsync(Player player)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsByNicknameAsync(string nickname)
        {
            throw new NotImplementedException();
        }

        public Task<Player?> GetByIdAsync(Guid playerId)
        {
            throw new NotImplementedException();
        }

        public Task<Player?> GetByNicknameAsync(string nickname)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Player>> GetOnlinePlayersAsync()
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Player player)
        {
            throw new NotImplementedException();
        }
    }
}
