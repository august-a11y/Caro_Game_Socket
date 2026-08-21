using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Interfaces;

namespace CaroGame.Infrastructure.Repositories
{
    public class InMemoryChallengeRepository : IChallengeRepository
    {
        private readonly ConcurrentDictionary<string, Challenge> _challenges = new();

        public Task AddAsync(Challenge challenge)
        {
            if (challenge == null)
                throw new ArgumentNullException(nameof(challenge));

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
                .Where(c => c.TargetPlayerId == playerId && c.Status == ChallengeStatus.Pending)
                .ToList();

            return Task.FromResult<IEnumerable<Challenge>>(pending);
        }

        public Task UpdateAsync(Challenge challenge)
        {
            if (challenge == null)
                throw new ArgumentNullException(nameof(challenge));

            if (!_challenges.ContainsKey(challenge.Id))
                throw new KeyNotFoundException($"Challenge with ID '{challenge.Id}' was not found.");

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

*Lưu ý: Bạn có thể copy toàn bộ đoạn mã trên và dán đè vào khung biên tập file trên GitHub.*
