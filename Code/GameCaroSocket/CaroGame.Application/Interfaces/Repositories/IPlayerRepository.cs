using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.Interfaces.Repositories
{
    public interface IPlayerRepository
    {
        Player? GetById(Guid playerId);

        Player? GetByNickname(string nickname);

        IReadOnlyList<Player> GetOnlinePlayers();

        void Add(Player player);

        void Update(Player player);
        void Remove(Guid playerId);

        bool ExistsByNickname(string nickname);
    }
}
