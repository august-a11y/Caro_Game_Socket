using CaroGame.Application.Interfaces.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.SessionUseCase
{
    public class UdpEndpointRegistrar : IUdpEndpointRegistrar
    {
        private readonly ISessionRepository _sessionRepository;

        public UdpEndpointRegistrar(ISessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository
                ?? throw new ArgumentNullException(nameof(sessionRepository));
        }

        public async Task RegisterAsync(Guid playerId, string address, int port, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var session = await _sessionRepository.GetByPlayerIdAsync(playerId);
            if (session is null || !session.IsConnected)
                return;

            cancellationToken.ThrowIfCancellationRequested();

            session.SetUdpEndpoint(address, port);
            await _sessionRepository.UpdateAsync(session);
        }
    }
}
