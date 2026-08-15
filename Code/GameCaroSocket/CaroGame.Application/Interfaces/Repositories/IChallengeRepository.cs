using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.Interfaces.Repositories
{
    public interface IChallengeRepository
    {
        Task<Challenge?> GetByIdAsync(Guid challengeId);

        Task<IReadOnlyList<Challenge>> GetPendingForPlayerAsync(
            Guid playerId);

        Task AddAsync(Challenge challenge);

        Task UpdateAsync(Challenge challenge);

        Task RemoveAsync(Guid challengeId);
    }
}
