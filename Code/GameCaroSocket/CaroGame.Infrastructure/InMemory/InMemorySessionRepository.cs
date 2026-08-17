using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CaroGame.Infrastructure.InMemory
{
    public class InMemorySessionRepository : ISessionRepository
    {
        private readonly ConcurrentDictionary<Guid, Session> _sessions = new ConcurrentDictionary<Guid, Session>();

        // AddAsync, GetByTokenAsync and GetByUserIdAsync are intentionally not implemented here
        // as per task instructions. Other code in the solution may provide those.

        public Task AddAsync(Session session)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(Guid playerId)
        {
            var exists = _sessions.ContainsKey(playerId);
            return Task.FromResult(exists);
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
            _sessions.TryRemove(userId, out _);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Session session)
        {
            // Replace or add the session for the given player id
            _sessions.AddOrUpdate(session.PlayerId, session, (k, old) => session);
            return Task.CompletedTask;
        }
    }
}
