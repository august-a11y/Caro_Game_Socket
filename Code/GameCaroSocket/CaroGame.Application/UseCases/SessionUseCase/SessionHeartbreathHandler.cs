using CaroGame.Application.Interfaces.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.SessionUseCase
{
    public class SessionHeartbreathHandler : ISessionHeartbreathHandler
    {
        private readonly ISessionRepository _sessionRepository;

        public SessionHeartbreathHandler(ISessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository;
        }

        public async Task HandleAsync(Guid playerId, CancellationToken cancellationToken)
        {
            var session = await _sessionRepository.GetByPlayerIdAsync(playerId);
            if (session != null)
            {
                session.UpdateHeartbeat();
                await _sessionRepository.UpdateAsync(session);
            }
        }
    }
}
