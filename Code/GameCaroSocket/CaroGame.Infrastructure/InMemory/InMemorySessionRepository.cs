using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace CaroGame.Infrastructure.InMemory
{
    public class InMemorySessionRepository : ISessionRepository
    {
        private readonly ConcurrentDictionary<Guid, Session> _sessions = new();

        public Task AddAsync(Session session)
        {
            _sessions[session.SessionId] = session;

            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid playerId)
        {
            throw new NotImplementedException();
        }

        public Task<Session?> GetByIdAsync(Guid sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);

            return Task.FromResult(session);
        }

        public Task<Session?> GetByPlayerIdAsync(Guid playerId)
        {
            var session = _sessions.Values
                .FirstOrDefault(s => s.PlayerId == playerId);

            return Task.FromResult(session);
        }

        public Task RemoveAsync(Guid PlayerId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Session session)
        {
            throw new NotImplementedException();
        }
    }
}