using CaroGame.Application.Contracts;
using CaroGame.Application.Interfaces.Repositories;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.Spectator
{
    public class GetOngoingRoomsHandler : IRequestHandler<GetOngoingRoomsQuery, List<RoomState>>
    {
        private readonly IRoomRepository _roomRepository;

        public GetOngoingRoomsHandler(IRoomRepository roomRepository)
        {
            _roomRepository = roomRepository;
        }

        public async Task<List<RoomState>> Handle(GetOngoingRoomsQuery request, CancellationToken cancellationToken)
        {
            var rooms = await _roomRepository.GetOngoingRoomsAsync();

            // Chuyển đổi từ Room (Domain) sang RoomState (Contract/DTO) để trả về cho Client
            return rooms.Select(room => new RoomState
            {
                RoomId = room.RoomId,
                PlayerA = room.PlayerA,
                PlayerB = room.PlayerB,
                Status = room.Status
            }).ToList();
        }
    }
}
