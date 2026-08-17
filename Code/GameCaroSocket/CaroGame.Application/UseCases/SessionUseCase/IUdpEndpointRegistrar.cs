namespace CaroGame.Application.UseCases.SessionUseCase
{
    public interface IUdpEndpointRegistrar
    {
        Task RegisterAsync(Guid playerId, string address, int port, CancellationToken cancellationToken = default);
    }
}

