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
            var exists = _sessions.Values.Any(s => s.PlayerId == playerId);

            return Task.FromResult(exists);
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
            var session = _sessions.Values.FirstOrDefault(s => s.PlayerId == PlayerId);

            if (session is not null)
            {
                _sessions.TryRemove(session.SessionId, out _);
            }
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Session session)
        {
            while (true)
            {
                if (!_sessions.TryGetValue(session.SessionId, out var existing))
                {
                    break;
                }

                if (_sessions.TryUpdate(session.SessionId, session, existing))
                {
                    break;
                }
            }
            return Task.CompletedTask;
        }
    }
}