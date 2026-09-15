using CaroGame.Server.Services;
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.MatchMaking;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Infrastructure.Networking.Messaging;
using CaroGame.Shared.Networking.Messaging;
using CaroGame.Shared.Protocol.Contracts;

namespace CaroGame.Server.Controllers;

public class MatchMakingController(
    IChallengeSender sender,
    IChallengeResponder responder,
    IChallengeCanceller canceller,
    IChallengeRepository challenges,
    IRoomRepository rooms,
    LobbyLockService lobbyLock,
    RequestExecutor requests,
    MessageService messages,
    TimeProvider time)
{
    public async Task SendChallengeAsync(ClientConnection connection, Packet packet, CancellationToken cancellationToken = default)
    {
        var request = requests.Deserialize<ChallengeRequest>(packet);
        Task delivery;
        await lobbyLock.Gate.WaitAsync(cancellationToken);
        try
        {
            var session = connection.RequireSession();
            if (!sender.SendChallenge(session.PlayerId.ToString(), request.OpponentId.ToString()))
                throw new InvalidOperationException("Both players must be free and have no pending invitation between them.");
            var challenge = challenges.GetPendingForPlayer(request.OpponentId)
                .Single(c => c.FromPlayerId == session.PlayerId && c.ToPlayerId == request.OpponentId);
            var response = Response(challenge, request.RequestId);
            var reply = messages.SendResponseAsync(connection, MessageTypes.ChallengeSendResponse, response, cancellationToken);
            var notification = messages.BroadcastAsync(new[] { challenge.ToPlayerId },
                MessageTypes.ChallengeReceivedNotification, response, cancellationToken);
            delivery = Task.WhenAll(reply, notification);
        }
        finally
        {
            lobbyLock.Gate.Release();
        }
        await delivery;
    }

    public async Task RespondToChallengeAsync(ClientConnection connection, Packet packet, CancellationToken cancellationToken = default)
    {
        var request = requests.Deserialize<ChallengeRespondRequest>(packet);
        Task broadcast;
        await lobbyLock.Gate.WaitAsync(cancellationToken);
        try
        {
            var session = connection.RequireSession();
            var challenge = challenges.GetById(request.ChallengeId)
                ?? throw new KeyNotFoundException("Challenge was not found.");
            if (challenge.ToPlayerId != session.PlayerId)
                throw new UnauthorizedAccessException("Only the invited player can respond.");
            if (challenge.Status != ChallengeStatus.Pending)
                throw new InvalidOperationException("Challenge is no longer pending.");

            var roomId = responder.Respond(challenge.FromPlayerId.ToString(), session.PlayerId.ToString(), request.Accept);
            if (challenge.Status == ChallengeStatus.Pending)
                throw new InvalidOperationException("The invitation cannot be accepted because a player is unavailable.");
            var room = roomId is null ? null : rooms.GetById(Guid.Parse(roomId));
            var response = Response(challenge, request.RequestId, room);
            var messageType = challenge.Status switch
            {
                ChallengeStatus.Accepted => MessageTypes.ChallengeAcceptedNotification,
                ChallengeStatus.Rejected => MessageTypes.ChallengeDeclinedNotification,
                _ => MessageTypes.ChallengeExpiredNotification
            };
            broadcast = messages.BroadcastAsync(new[] { challenge.FromPlayerId, challenge.ToPlayerId },
                messageType, response, cancellationToken);
        }
        finally
        {
            lobbyLock.Gate.Release();
        }
        await broadcast;
    }

    public async Task CancelChallengeAsync(ClientConnection connection, Packet packet, CancellationToken cancellationToken = default)
    {
        var request = requests.Deserialize<ChallengeReferenceRequest>(packet);
        Task broadcast;
        await lobbyLock.Gate.WaitAsync(cancellationToken);
        try
        {
            var session = connection.RequireSession();
            var challenge = canceller.Cancel(request.ChallengeId, session.PlayerId);
            var messageType = challenge.Status == ChallengeStatus.Expired
                ? MessageTypes.ChallengeExpiredNotification : MessageTypes.ChallengeCancelledNotification;
            broadcast = messages.BroadcastAsync(new[] { challenge.FromPlayerId, challenge.ToPlayerId },
                messageType, Response(challenge, request.RequestId), cancellationToken);
        }
        finally
        {
            lobbyLock.Gate.Release();
        }
        await broadcast;
    }

    private ChallengeResponse Response(Challenge challenge, Guid? requestId, Room? room = null) => new(
        requestId, new ChallengeDto(challenge.ChallengeId, challenge.FromPlayerId, challenge.ToPlayerId,
            challenge.ExpiresAt, challenge.Status.ToString()),
        room is null ? null : RoomMessages.Snapshot(room, time.GetUtcNow().UtcDateTime));
}
