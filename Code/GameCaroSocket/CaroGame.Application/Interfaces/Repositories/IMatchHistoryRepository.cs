using CaroGame.Application.Contracts;

namespace CaroGame.Application.Interfaces.Repositories;

public interface IMatchHistoryRepository
{
    IReadOnlyList<MatchHistoryEntry> GetAll();

    void Add(MatchHistoryEntry entry);
}
