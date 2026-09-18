namespace Caro.Client.WinForms.Core;

internal sealed class ClientStateStore
{
    private readonly object _sync = new();
    private ClientState _current = new();
    public ClientState Current { get { lock (_sync) return _current; } }

    public void Update(Func<ClientState, ClientState> update)
    {
        lock (_sync) _current = update(_current);
    }
}
