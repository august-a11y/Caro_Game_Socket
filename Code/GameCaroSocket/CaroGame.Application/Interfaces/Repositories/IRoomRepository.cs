using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.Interfaces.Repositories
{
    public interface IRoomRepository
    {
        Task<Room?> GetByIdAsync(Guid roomId);

        Task<IReadOnlyList<Room>> GetOngoingRoomsAsync();

        Task AddAsync(Room room);

        Task UpdateAsync(Room room);

        Task RemoveAsync(Guid roomId);
    }
}
