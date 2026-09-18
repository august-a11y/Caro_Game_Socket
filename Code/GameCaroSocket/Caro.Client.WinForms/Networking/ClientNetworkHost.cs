using CaroGame.Shared.Networking.Messaging;

namespace Caro.Client.WinForms.Networking;

using Caro.Client.WinForms.Core;

using CaroGame.Shared.Protocol.Contracts;

internal sealed class ClientNetworkHost : IAsyncDisposable
{
    public TcpGameClient Connection { get; }
    public MessageRouter Router { get; }
    public ServerDiscoveryClient Discovery { get; }
    public ClientLogger Logger => ClientLogger.Shared;
    public event Action<ErrorResponse>? ErrorReceived;

    public ClientNetworkHost()
    {
        Logger.Info("Client network host created.");
        var serializer = new JsonMessageSerializer();
        Router = new MessageRouter(serializer);
        Router.Register<ErrorResponse>(MessageTypes.ErrorResponse, (error, _) =>
        {
            ErrorReceived?.Invoke(error);
            return Task.CompletedTask;
        });
        Router.Register<ErrorResponse>(MessageTypes.MoveRejected, (error, _) =>
        {
            ErrorReceived?.Invoke(error);
            return Task.CompletedTask;
        });
        Connection = new TcpGameClient(new PacketFramer(), serializer);
        Connection.PacketReceived += Router.RouteAsync;
        Discovery = new ServerDiscoveryClient();
    }

    public Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default) =>
        Connection.ConnectAsync(host, port, cancellationToken);

    public Task SendAsync<TPayload>(MessageTypes type, TPayload payload,
        CancellationToken cancellationToken = default) =>
        Connection.SendAsync(type, payload, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        Logger.Info("Client network host disposing.");
        Connection.PacketReceived -= Router.RouteAsync;
        await Discovery.DisposeAsync();
        await Connection.DisposeAsync();
    }
}
