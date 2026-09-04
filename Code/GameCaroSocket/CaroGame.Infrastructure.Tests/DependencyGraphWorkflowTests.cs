using CaroGame.Application;
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.GamePlay;
using CaroGame.Application.UseCases.Match;
using CaroGame.Application.UseCases.MatchMaking;
using CaroGame.Application.UseCases.SessionUseCase;
using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;
using CaroGame.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CaroGame.Infrastructure.Tests;

public sealed class DependencyGraphWorkflowTests
{
    [Fact]
    public async Task RegisteredGraph_SupportsJoinChallengeReadyMoveDisconnectAndAuthenticatedReconnect()
    {
        var services = new ServiceCollection()
            .AddCaroGameApplication()
            .AddCaroGameInfrastructure();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        var joiner = provider.GetRequiredService<IPlayerJoiner>();
        var challengerSession = await joiner.JoinAsync("challenger", CancellationToken.None);
        var opponentSession = await joiner.JoinAsync("opponent", CancellationToken.None);

        var sender = provider.GetRequiredService<IChallengeSender>();
        Assert.True(await sender.SendChallengeAsync(
            challengerSession.PlayerId.ToString(),
            opponentSession.PlayerId.ToString()));

        var responder = provider.GetRequiredService<IChallengeResponder>();
        var roomToken = await responder.RespondAsync(
            challengerSession.PlayerId.ToString(),
            opponentSession.PlayerId.ToString(),
            accept: true);
        Assert.True(Guid.TryParse(roomToken, out var roomId));

        var readyHandler = provider.GetRequiredService<IPlayerReadyHandler>();
        var waitingRoom = await readyHandler.HandleAsync(
            roomId,
            challengerSession.PlayerId);
        Assert.Equal(RoomStatus.Waiting, waitingRoom.Status);

        var playingRoom = await readyHandler.HandleAsync(
            roomId,
            opponentSession.PlayerId);
        Assert.Equal(RoomStatus.Playing, playingRoom.Status);
        Assert.NotNull(playingRoom.CurrentMatch);

        var moveSubmitter = provider.GetRequiredService<IMoveSubmitter>();
        await moveSubmitter.SubmitMoveAsync(
            roomId,
            challengerSession.PlayerId,
            new Position(0, 0),
            CancellationToken.None);

        var disconnectHandler = provider.GetRequiredService<IPlayerDisconnectHandler>();
        await disconnectHandler.HandleAsync(opponentSession.PlayerId, CancellationToken.None);
        Assert.True(playingRoom.CurrentMatch!.TurnManager.IsPaused);
        Assert.False(opponentSession.IsConnected);

        var reconnector = provider.GetRequiredService<IPlayerReconnector>();
        var reconnected = await reconnector.ReconnectPlayerAsync(
            opponentSession.PlayerId,
            opponentSession.SessionId,
            CancellationToken.None);

        Assert.Same(opponentSession, reconnected);
        Assert.True(reconnected.IsConnected);
        Assert.False(playingRoom.CurrentMatch.TurnManager.IsPaused);
        Assert.Equal(
            PlayerStatus.InMatch,
            (await provider.GetRequiredService<IPlayerRepository>()
                .GetByIdAsync(opponentSession.PlayerId))!.Status);
    }
}
