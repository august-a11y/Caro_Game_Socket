using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.Interfaces.Repositories
{
    public interface ISessionRepository
    {
        Task<Session?> GetByPlayerIdAsync(Guid playerId);

        Task<Session?> GetByIdAsync(Guid sessionId);

        Task AddAsync(Session session);

        Task UpdateAsync(Session session);

        Task RemoveAsync(Guid playerId);

        Task<bool> ExistsAsync(Guid playerId);
    }
}
