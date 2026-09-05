using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System.Collections.Concurrent;

namespace CaroGame.Infrastructure.InMemory;

public sealed class InMemorySessionRepository : ISessionRepository
{
    private readonly ConcurrentDictionary<Guid, Session> _sessions = new();
    private readonly object _sync = new();

    public Task AddAsync(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        lock (_sync)
        {
            if (_sessions.ContainsKey(session.SessionId))
                throw new InvalidOperationException($"Session with ID '{session.SessionId}' already exists.");
            EnsurePlayerHasNoOtherSession(session);

            if (!_sessions.TryAdd(session.SessionId, session))
                throw new InvalidOperationException($"Session with ID '{session.SessionId}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid playerId)
    {
        lock (_sync)
        {
            return Task.FromResult(_sessions.Values.Any(session => session.PlayerId == playerId));
        }
    }

    public Task<Session?> GetByIdAsync(Guid sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task<Session?> GetByPlayerIdAsync(Guid playerId)
    {
        lock (_sync)
        {
            return Task.FromResult(_sessions.Values.FirstOrDefault(session => session.PlayerId == playerId));
        }
    }

    public Task RemoveAsync(Guid playerId)
    {
        lock (_sync)
        {
            var session = _sessions.Values.FirstOrDefault(candidate => candidate.PlayerId == playerId);
            if (session is not null)
                _sessions.TryRemove(session.SessionId, out _);
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        lock (_sync)
        {
            if (!_sessions.TryGetValue(session.SessionId, out var existing))
                throw new KeyNotFoundException($"Session with ID '{session.SessionId}' was not found.");
            if (existing.PlayerId != session.PlayerId)
                throw new InvalidOperationException("Session ownership cannot be changed.");

            EnsurePlayerHasNoOtherSession(session);
            _sessions[session.SessionId] = session;
        }

        return Task.CompletedTask;
    }

    private void EnsurePlayerHasNoOtherSession(Session session)
    {
        if (_sessions.Values.Any(existing =>
            existing.PlayerId == session.PlayerId && existing.SessionId != session.SessionId))
        {
            throw new InvalidOperationException("The player already has a session.");
        }
    }
}
