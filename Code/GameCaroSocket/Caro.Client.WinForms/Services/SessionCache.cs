using System.Text.Json;
using Caro.Client.WinForms.Core;
using CaroGame.Shared.Protocol.Contracts;

namespace Caro.Client.WinForms.Services;

internal sealed record SavedSession(string Host, int Port, Guid PlayerId, Guid SessionId, string Nickname)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(Host) && Port is >= 1 and <= 65535 &&
        PlayerId != Guid.Empty && SessionId != Guid.Empty && !string.IsNullOrWhiteSpace(Nickname);

    public ClientState ToDisconnectedState() => new()
    {
        PlayerId = PlayerId, SessionId = SessionId,
        CurrentPlayer = new PlayerDto(PlayerId, Nickname, "Offline")
    };
}

internal sealed class SessionCache
{
    private readonly string _path;

    public SessionCache(string? path = null)
    {
        _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GameCaroSocket", "session.json");
    }

    public SavedSession? Load()
    {
        try
        {
            if (!File.Exists(_path)) return null;
            var saved = JsonSerializer.Deserialize<SavedSession>(File.ReadAllText(_path));
            return saved?.IsValid == true ? saved : null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            // An absent/unreadable cache must not prevent starting a new session.
            return null;
        }
    }

    public void Save(SavedSession saved)
    {
        if (!saved.IsValid) throw new ArgumentException("Invalid session credentials.", nameof(saved));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
        string temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(saved));
            File.Move(temporary, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public void Clear()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }
}
