using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Infrastructure.InMemory
{
    public class InMemoryRoomRepository : IRoomRepository
    {
        public Task AddAsync(Room room)
        {
            throw new NotImplementedException();
        }

        public Task<Room?> GetByIdAsync(Guid roomId)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Room>> GetOngoingRoomsAsync()
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(Guid roomId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Room room)
        {
            throw new NotImplementedException();
        }
    }
}
