using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Infrastructure.InMemory
{
    public class InMemorySessionRepository : ISessionRepository
    {
        public Task AddAsync(Session session)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(Guid playerId)
        {
            throw new NotImplementedException();
        }

        public Task<Session?> GetByTokenAsync(Guid sessionId)
        {
            throw new NotImplementedException();
        }

        public Task<Session?> GetByUserIdAsync(Guid playerId)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Session session)
        {
            throw new NotImplementedException();
        }
    }
}
