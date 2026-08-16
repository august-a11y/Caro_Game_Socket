using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Infrastructure.InMemory
{
    public class InMemorySessionRepository : ISessionRepository
    {
        private readonly Dictionary<Guid, Session> _sessions = new();

        public Task AddAsync(Session session)
        {
            _sessions[session.SessionId] = session;

            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid playerId)
        {
            throw new NotImplementedException();
        }

        public Task<Session?> GetByTokenAsync(Guid sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);

            return Task.FromResult(session);
        }

        public Task<Session?> GetByUserIdAsync(Guid playerId)
        {
            var session = _sessions.Values
                .FirstOrDefault(s => s.PlayerId == playerId);

            return Task.FromResult(session);
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