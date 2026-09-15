using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.Interfaces.Repositories
{
    public interface ISessionRepository
    {
        Session? GetByPlayerId(Guid playerId);
        IReadOnlyList<Session> GetAll();

        Session? GetById(Guid sessionId);

        void Add(Session session);

        void Update(Session session);

        void Remove(Guid playerId);

        bool Exists(Guid playerId);
    }
}
