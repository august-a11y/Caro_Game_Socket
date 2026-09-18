using System.Collections.Concurrent;
using Caro.Client.WinForms.Core;
using Caro.Client.WinForms.Networking;
using CaroGame.Shared.Networking.Messaging;
using CaroGame.Shared.Protocol.Contracts;

namespace Caro.Client.WinForms.Services;

/// <summary>
/// Correlates client requests with responses received by the shared message router.
/// </summary>
internal sealed class ClientRequestManager : IDisposable
{
    private sealed record Pending(
        IReadOnlySet<MessageTypes> ResponseTypes,
        TaskCompletionSource<object> Completion);

    private readonly ClientNetworkHost _network;
    private readonly ClientLogger _logger = ClientLogger.Shared;
    private readonly TimeSpan _timeout;
    private readonly Func<ErrorResponse, Exception> _errorFactory;
    private readonly ConcurrentDictionary<Guid, Pending> _pending = new();
    private readonly CancellationTokenSource _stop = new();
    private bool _disposed;

    public ClientRequestManager(
        ClientNetworkHost network,
        TimeSpan timeout,
        Func<ErrorResponse, Exception>? errorFactory = null)
    {
        _network = network ?? throw new ArgumentNullException(nameof(network));
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));
        _timeout = timeout;
        _errorFactory = errorFactory ?? (error => new InvalidOperationException(error.Message));
        _network.ErrorReceived += OnError;
        _network.Connection.Disconnected += OnDisconnected;
    }

    public async Task<TResponse> SendAsync<TRequest, TResponse>(
        MessageTypes requestType,
        IReadOnlyCollection<MessageTypes> responseTypes,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : RequestMessage
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(responseTypes);
        ArgumentNullException.ThrowIfNull(request);
        if (responseTypes.Count == 0)
            throw new ArgumentException("Phải có ít nhất một response type.", nameof(responseTypes));

        var id = request.RequestId
            ?? throw new ArgumentException("RequestId không được rỗng.", nameof(request));
        var completion = new TaskCompletionSource<object>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = new Pending(responseTypes.ToHashSet(), completion);
        _logger.Debug($"Request registered: {requestType}, id={id}, responses={string.Join(',', responseTypes)}");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _stop.Token);
        timeout.CancelAfter(_timeout);
        try
        {
            await _network.SendAsync(requestType, request, timeout.Token);
            _logger.Debug($"Request sent: {requestType}, id={id}");
            return (TResponse)await completion.Task.WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested && !_stop.IsCancellationRequested)
        {
            _logger.Warning($"Request timed out: {requestType}, id={id}");
            throw new TimeoutException(
                $"Máy chủ chưa phản hồi {requestType} trong {_timeout.TotalSeconds:0.#} giây.");
        }
        finally
        {
            _pending.TryRemove(id, out _);
            _logger.Debug($"Request completed/removed: {requestType}, id={id}");
        }
    }

    public bool TryComplete<TResponse>(
        Guid? requestId, MessageTypes responseType, TResponse response)
    {
        if (requestId is not Guid id ||
            !_pending.TryGetValue(id, out var pending) ||
            !pending.ResponseTypes.Contains(responseType))
            return false;

        return pending.Completion.TrySetResult(response!);
    }

    private void OnError(ErrorResponse error)
    {
        if (error.RequestId is Guid id && _pending.TryGetValue(id, out var pending))
            pending.Completion.TrySetException(_errorFactory(error));
    }

    private void OnDisconnected()
    {
        foreach (var pending in _pending.Values)
            pending.Completion.TrySetException(new IOException("Đã mất kết nối máy chủ."));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _stop.Cancel();
        _network.ErrorReceived -= OnError;
        _network.Connection.Disconnected -= OnDisconnected;
    }
}
