using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CaroGame.Infrastructure.Repositories
{
    public class InMemoryRoomRepository : IRoomRepository
    {
        // Sử dụng ConcurrentDictionary để đảm bảo Thread-Safe (an toàn khi nhiều luồng truy cập cùng lúc)
        private readonly ConcurrentDictionary<Guid, Room> _rooms = new();

        public Task<Room?> GetByIdAsync(Guid roomId)
        {
            _rooms.TryGetValue(roomId, out var room);
            return Task.FromResult(room);
        }

        public Task<IReadOnlyList<Room>> GetOngoingRoomsAsync()
        {
            var ongoingRooms = _rooms.Values
                .Where(r => r.Status == CaroGame.Domain.Enum.RoomStatus.Playing || 
                            r.Status == CaroGame.Domain.Enum.RoomStatus.Waiting)
                .ToList();

            return Task.FromResult<IReadOnlyList<Room>>(ongoingRooms);
        }

        public Task AddAsync(Room room)
        {
            _rooms.TryAdd(room.RoomId, room);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Room room)
        {
            // Trong bộ nhớ RAM, đối tượng room đã được tham chiếu trực tiếp.
            // Việc thay đổi thuộc tính của room ở đâu đó (VD: room.AddSpectator) 
            // tự động phản ánh vào _rooms. Hàm Update này đóng vai trò tuân thủ Interface.
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid roomId)
        {
            _rooms.TryRemove(roomId, out _);
            return Task.CompletedTask;
        }
    }
}
