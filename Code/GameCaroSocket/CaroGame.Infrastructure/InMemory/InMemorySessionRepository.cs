using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System.Collections.Concurrent;

namespace CaroGame.Infrastructure.InMemory;

// Callers coordinate entity changes and multi-step operations using shared lobby/room locks.
// ConcurrentDictionary protects individual dictionary operations only.
public sealed class InMemorySessionRepository : ISessionRepository
{
    private readonly ConcurrentDictionary<Guid, Session> _sessions = new();
    public IReadOnlyList<Session> GetAll() => _sessions.Values.ToArray();

    public void Add(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (_sessions.ContainsKey(session.SessionId))
            throw new InvalidOperationException($"Session with ID '{session.SessionId}' already exists.");
        EnsurePlayerHasNoOtherSession(session);

        if (!_sessions.TryAdd(session.SessionId, session))
            throw new InvalidOperationException($"Session with ID '{session.SessionId}' already exists.");
    }

    public bool Exists(Guid playerId)
    {
        return _sessions.Values.Any(session => session.PlayerId == playerId);
    }

    public Session? GetById(Guid sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    public Session? GetByPlayerId(Guid playerId)
    {
        return _sessions.Values.FirstOrDefault(session => session.PlayerId == playerId);
    }

    public void Remove(Guid playerId)
    {
        var session = _sessions.Values.FirstOrDefault(candidate => candidate.PlayerId == playerId);
        if (session is not null)
            _sessions.TryRemove(session.SessionId, out _);
    }

    public void Update(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!_sessions.TryGetValue(session.SessionId, out var existing))
            throw new KeyNotFoundException($"Session with ID '{session.SessionId}' was not found.");
        if (existing.PlayerId != session.PlayerId)
            throw new InvalidOperationException("Session ownership cannot be changed.");

        EnsurePlayerHasNoOtherSession(session);
        _sessions[session.SessionId] = session;
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
