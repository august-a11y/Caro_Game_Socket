using CaroGame.Application.Interfaces.Repositories;
using System;

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

        public void Register(Guid playerId, string address, int port)
        {
            var session = _sessionRepository.GetByPlayerId(playerId);
            if (session is null || !session.IsConnected)
                return;


            session.SetUdpEndpoint(address, port);
            _sessionRepository.Update(session);
        }
    }
}
