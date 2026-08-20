using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CaroGame.Infrastructure.Repositories
{
    public class Challenge
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ChallengerId { get; set; } = string.Empty;
        public string TargetPlayerId { get; set; } = string.Empty;
        public ChallengeStatus Status { get; set; } = ChallengeStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum ChallengeStatus
    {
        Pending,
        Accepted,
        Rejected,
        Expired,
        Cancelled
    }

    public interface IChallengeRepository
    {
        Task AddAsync(Challenge challenge);
        Task<Challenge?> GetByIdAsync(string id);
        Task<IEnumerable<Challenge>> GetPendingChallengesForPlayerAsync(string playerId);
        Task UpdateAsync(Challenge challenge);
        Task DeleteAsync(string id);
    }

    public class InMemoryChallengeRepository : IChallengeRepository
    {
        private readonly ConcurrentDictionary<string, Challenge> _challenges = new();

        public Task AddAsync(Challenge challenge)
        {
            _challenges[challenge.Id] = challenge;
            return Task.CompletedTask;
        }

        public Task<Challenge?> GetByIdAsync(string id)
        {
            _challenges.TryGetValue(id, out var challenge);
            return Task.FromResult(challenge);
        }

        public Task<IEnumerable<Challenge>> GetPendingChallengesForPlayerAsync(string playerId)
        {
            var pending = _challenges.Values
                .Where(c => c.TargetPlayerId == playerId && c.Status == ChallengeStatus.Pending);
            
            return Task.FromResult(pending);
        }

        public Task UpdateAsync(Challenge challenge)
        {
            _challenges[challenge.Id] = challenge;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            _challenges.TryRemove(id, out _);
            return Task.CompletedTask;
        }
    }
}

