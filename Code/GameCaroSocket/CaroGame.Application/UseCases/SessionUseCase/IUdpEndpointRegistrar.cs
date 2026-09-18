namespace CaroGame.Application.UseCases.SessionUseCase
{
    public interface IUdpEndpointRegistrar
    {
        void Register(Guid playerId, string address, int port);
    }
}

