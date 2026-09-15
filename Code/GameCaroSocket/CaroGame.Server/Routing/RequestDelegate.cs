using CaroGame.Infrastructure.Networking.Messaging;

namespace CaroGame.Server.Routing;

public delegate Task RequestDelegate(ClientConnection connection, Packet packet, CancellationToken cancellationToken);
