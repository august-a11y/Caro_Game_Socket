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

        public Task AddAsync(Session session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            // Use PlayerId as the dictionary key. Do not allow duplicate sessions for the same player.
            var added = _sessions.TryAdd(session.PlayerId, session);
            if (!added)
                throw new InvalidOperationException("A session for this player already exists.");

            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid playerId)
        {
            var exists = _sessions.ContainsKey(playerId);
            return Task.FromResult(exists);
        }

        public Task<Session?> GetByTokenAsync(Guid sessionId)
        {
            // The repository stores sessions keyed by PlayerId. To find by session token/id,
            // search the values for a matching SessionId. This assumes Session.SessionId is the token.
            foreach (var kv in _sessions)
            {
                var s = kv.Value;
                if (s.SessionId == sessionId)
                    return Task.FromResult<Session?>(s);
            }

            return Task.FromResult<Session?>(null);
        }

        public Task<Session?> GetByUserIdAsync(Guid playerId)
        {
            if (_sessions.TryGetValue(playerId, out var session))
                return Task.FromResult<Session?>(session);

            return Task.FromResult<Session?>(null);
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
