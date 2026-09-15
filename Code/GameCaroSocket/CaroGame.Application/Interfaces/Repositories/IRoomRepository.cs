using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.Interfaces.Repositories
{
    public interface IRoomRepository
    {
        Room? GetById(Guid roomId);
        IReadOnlyList<Room> GetAll();

        IReadOnlyList<Room> GetOngoingRooms();

        void Add(Room room);

        void Update(Room room);

        void Remove(Guid roomId);
    }
}
