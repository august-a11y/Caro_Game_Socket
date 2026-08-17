using CaroGame.Application.Interfaces.Repositories;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.Spectator
{
    public class LeaveAsSpectatorHandler : IRequestHandler<LeaveAsSpectatorCommand, bool>
    {
        private readonly IRoomRepository _roomRepository;

        public LeaveAsSpectatorHandler(IRoomRepository roomRepository)
        {
            _roomRepository = roomRepository;
        }

        public async Task<bool> Handle(LeaveAsSpectatorCommand request, CancellationToken cancellationToken)
        {
            var room = await _roomRepository.GetByIdAsync(request.RoomId);
            if (room == null)
            {
                return false;
            }

            room.RemoveSpectator(request.UserId);
            await _roomRepository.UpdateAsync(room);

            return true;
        }
    }
}
