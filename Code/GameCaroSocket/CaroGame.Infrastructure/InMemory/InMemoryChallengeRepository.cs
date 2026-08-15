using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Infrastructure.InMemory
{
    public class InMemoryChallengeRepository : IChallengeRepository
    {
        public Task AddAsync(Challenge challenge)
        {
            throw new NotImplementedException();
        }

        public Task<Challenge?> GetByIdAsync(Guid challengeId)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Challenge>> GetPendingForPlayerAsync(Guid playerId)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(Guid challengeId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Challenge challenge)
        {
            throw new NotImplementedException();
        }
    }
}
