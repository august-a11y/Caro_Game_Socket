using System.Collections.Concurrent;
using System.Diagnostics;
using Caro.Client.WinForms.Core;
using Caro.Client.WinForms.Networking;
using CaroGame.Shared.Networking.Messaging;
using CaroGame.Shared.Protocol.Contracts;

namespace Caro.Client.WinForms.Services;

internal sealed class SessionRequestException(ErrorResponse error) : Exception(error.Message)
{
    public string ErrorCode { get; } = error.ErrorCode;
}

internal sealed class SessionClientService : IAsyncDisposable
{
    private sealed record Pending(MessageTypes ResponseType, TaskCompletionSource<object> Completion);
    private readonly ClientNetworkHost _network;
    private readonly ClientStateStore _state;
    private readonly ConcurrentDictionary<Guid, Pending> _pending = new();
    private readonly SemaphoreSlim _sessionGate = new(1, 1);
    private readonly SemaphoreSlim _heartbeatGate = new(1, 1);
    private readonly object _heartbeatSync = new();
    private readonly TimeSpan _heartbeatInterval;
    private readonly TimeSpan _requestTimeout;
    private CancellationTokenSource? _heartbeatStop;
    private Task _heartbeatTask = Task.CompletedTask;
    private bool _disposed;

    public event Action<Exception>? HeartbeatFailed;

    public SessionClientService(ClientNetworkHost network, ClientStateStore state,
        TimeSpan? heartbeatInterval = null, TimeSpan? requestTimeout = null)
    {
        _network = network;
        _state = state;
        _heartbeatInterval = heartbeatInterval ?? TimeSpan.FromSeconds(5);
        _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(10);
        if (_heartbeatInterval <= TimeSpan.Zero || _requestTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(
                _heartbeatInterval <= TimeSpan.Zero ? nameof(heartbeatInterval) : nameof(requestTimeout));
        network.Router.Register<SessionResponse>(MessageTypes.PlayerJoinResponse,
            (response, _) => Complete(response.RequestId, MessageTypes.PlayerJoinResponse, response));
        network.Router.Register<SessionResponse>(MessageTypes.PlayerReconnectResponse,
            (response, _) => Complete(response.RequestId, MessageTypes.PlayerReconnectResponse, response));
        network.Router.Register<HeartbeatResponse>(MessageTypes.HeartbeatResponse,
            (response, _) => Complete(response.RequestId, MessageTypes.HeartbeatResponse, response));
        network.Router.Register<UdpEndpointResponse>(MessageTypes.RegisterUdpEndpointResponse,
            (response, _) => Complete(response.RequestId, MessageTypes.RegisterUdpEndpointResponse, response));
        network.ErrorReceived += OnError;
        network.Connection.Disconnected += OnDisconnected;
    }

    public Task<SessionResponse> JoinAsync(string nickname, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);
        return EstablishAsync(MessageTypes.PlayerJoinRequest, MessageTypes.PlayerJoinResponse,
            new PlayerJoinRequest { RequestId = Guid.NewGuid(), Nickname = nickname.Trim() }, cancellationToken);
    }

    private void OnError(ErrorResponse error)
    {
        if (error.RequestId is Guid id && _pending.TryGetValue(id, out var pending))
            pending.Completion.TrySetException(new SessionRequestException(error));
    }

    public Task<SessionResponse> ReconnectAsync(Guid playerId, Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (playerId == Guid.Empty || sessionId == Guid.Empty)
            throw new ArgumentException("PlayerId và SessionId không được rỗng.");
        return EstablishAsync(MessageTypes.PlayerReconnectRequest, MessageTypes.PlayerReconnectResponse,
            new PlayerReconnectRequest { RequestId = Guid.NewGuid(), PlayerId = playerId, SessionId = sessionId },
            cancellationToken);
    }

    private async Task<SessionResponse> EstablishAsync<T>(MessageTypes type, MessageTypes responseType,
        T request, CancellationToken cancellationToken) where T : RequestMessage
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!await _sessionGate.WaitAsync(0, cancellationToken))
            throw new InvalidOperationException("Đang xử lý một yêu cầu phiên khác.");
        try
        {
            if (_state.Current.IsJoined)
                throw new InvalidOperationException("Kết nối này đã có phiên.");
            await StopHeartbeatAsync();
            SessionResponse response;
            try
            {
                response = await RequestAsync<SessionResponse, T>(type, responseType, request, cancellationToken);
            }
            catch
            {
                // The server may have accepted a join before its response timed out.
                await _network.Connection.DisconnectAsync();
                throw;
            }
            _state.Update(_ => new ClientState
            {
                IsJoined = _network.Connection.IsConnected,
                PlayerId = response.PlayerId, SessionId = response.SessionId,
                CurrentPlayer = response.Player, Room = response.Room, LastRoom = response.LastRoom
            });
            if (!_state.Current.IsJoined)
                throw new IOException("Máy chủ đã đóng kết nối.");
            lock (_heartbeatSync)
            {
                _heartbeatStop = new CancellationTokenSource();
                _heartbeatTask = HeartbeatLoopAsync(_heartbeatStop.Token);
            }
            return response;
        }
        finally { _sessionGate.Release(); }
    }

    public async Task<HeartbeatResponse> HeartbeatAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _heartbeatGate.WaitAsync(cancellationToken);
        try
        {
            if (!_state.Current.IsJoined)
                throw new InvalidOperationException("Hãy Join hoặc Reconnect trước khi gửi heartbeat.");
            var started = Stopwatch.GetTimestamp();
            var response = await RequestAsync<HeartbeatResponse, RequestMessage>(MessageTypes.Heartbeat,
                MessageTypes.HeartbeatResponse, new RequestMessage { RequestId = Guid.NewGuid() }, cancellationToken);
            _state.Update(state => state with
            {
                LastHeartbeatAt = DateTime.UtcNow, ServerTime = response.ServerTime,
                RoundTripTime = Stopwatch.GetElapsedTime(started)
            });
            return response;
        }
        finally { _heartbeatGate.Release(); }
    }

    /// <summary>
    /// Registers the local UDP port after a session has been established.
    /// The server derives the public/local address from the TCP connection.
    /// </summary>
    public async Task<UdpEndpointResponse> RegisterUdpEndpointAsync(
        int port, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535);
        if (!_state.Current.IsJoined)
            throw new InvalidOperationException("Hãy Join hoặc Reconnect trước khi đăng ký cổng UDP.");

        return await RequestAsync<UdpEndpointResponse, RegisterUdpEndpointRequest>(
            MessageTypes.RegisterUdpEndpointRequest,
            MessageTypes.RegisterUdpEndpointResponse,
            new RegisterUdpEndpointRequest { RequestId = Guid.NewGuid(), Port = port },
            cancellationToken);
    }

    private async Task<TResponse> RequestAsync<TResponse, TRequest>(MessageTypes type,
        MessageTypes responseType, TRequest request, CancellationToken cancellationToken) where TRequest : RequestMessage
    {
        var id = request.RequestId!.Value;
        var completion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = new Pending(responseType, completion);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_requestTimeout);
        try
        {
            await _network.SendAsync(type, request, timeout.Token);
            return (TResponse)await completion.Task.WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Máy chủ không phản hồi {type} trong {_requestTimeout.TotalSeconds:0.#} giây.");
        }
        finally { _pending.TryRemove(id, out _); }
    }

    private Task Complete(Guid? id, MessageTypes type, object response)
    {
        if (id is Guid key && _pending.TryGetValue(key, out var pending) && pending.ResponseType == type)
            pending.Completion.TrySetResult(response);
        return Task.CompletedTask;
    }

    private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(_heartbeatInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
                await HeartbeatAsync(cancellationToken);
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception error)
        {
            await _network.Connection.DisconnectAsync();
            HeartbeatFailed?.Invoke(error);
        }
    }

    private void OnDisconnected()
    {
        lock (_heartbeatSync) _heartbeatStop?.Cancel();
        // Keep credentials and snapshots for reconnect, but block session commands.
        _state.Update(state => state with { IsJoined = false });
        foreach (var pending in _pending.Values)
            pending.Completion.TrySetException(new IOException("Đã mất kết nối máy chủ."));
    }

    private async Task StopHeartbeatAsync()
    {
        lock (_heartbeatSync) _heartbeatStop?.Cancel();
        await _heartbeatTask;
        lock (_heartbeatSync)
        {
            _heartbeatStop?.Dispose();
            _heartbeatStop = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await StopHeartbeatAsync();
        await _network.Connection.DisconnectAsync();
        _network.Connection.Disconnected -= OnDisconnected;
        _network.ErrorReceived -= OnError;
    }
}
