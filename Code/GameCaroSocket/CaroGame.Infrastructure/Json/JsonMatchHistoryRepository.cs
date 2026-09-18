using System.Text.Json;
using CaroGame.Application.Contracts;
using CaroGame.Application.Interfaces.Repositories;

namespace CaroGame.Infrastructure.Json;

public sealed class JsonMatchHistoryRepository : IMatchHistoryRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly object _sync = new();
    private readonly string _filePath;
    private readonly List<MatchHistoryEntry> _entries;

    public JsonMatchHistoryRepository(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = Path.GetFullPath(filePath);
        _entries = Load();
    }

    public IReadOnlyList<MatchHistoryEntry> GetAll()
    {
        lock (_sync)
            return _entries.ToArray();
    }

    public void Add(MatchHistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_sync)
        {
            _entries.Add(entry);
            Persist();
        }
    }

    private List<MatchHistoryEntry> Load()
    {
        if (!File.Exists(_filePath))
            return [];

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<MatchHistoryEntry>>(json, SerializerOptions) ?? [];
    }

    private void Persist()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var temporaryPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            var json = JsonSerializer.Serialize(_entries, SerializerOptions);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
