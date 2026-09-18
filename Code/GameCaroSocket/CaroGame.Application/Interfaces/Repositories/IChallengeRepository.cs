using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.Interfaces.Repositories
{
    public interface IChallengeRepository
    {
        Challenge? GetById(Guid challengeId);
        IReadOnlyList<Challenge> GetAll();

        IReadOnlyList<Challenge> GetPending();

        IReadOnlyList<Challenge> GetPendingForPlayer(
            Guid playerId);

        void Add(Challenge challenge);

        void Update(Challenge challenge);

        void Remove(Guid challengeId);
    }
}
