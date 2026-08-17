using CaroGame.Application.Contracts;
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.Spectator
{
    public class JoinAsSpectatorHandler : IRequestHandler<JoinAsSpectatorCommand, RoomState?>
    {
        private readonly IRoomRepository _roomRepository;

        public JoinAsSpectatorHandler(IRoomRepository roomRepository)
        {
            _roomRepository = roomRepository;
        }

        public async Task<RoomState?> Handle(JoinAsSpectatorCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm phòng dựa vào ID
            var room = await _roomRepository.GetByIdAsync(request.RoomId);
            if (room == null)
            {
                return null; // Không tìm thấy phòng
            }

            // 2. Thêm người dùng vào danh sách Khán giả của phòng
            room.AddSpectator(request.UserId);
            
            // Cập nhật lại Repo (dù In-Memory không bắt buộc nhưng vẫn gọi cho chuẩn Interface)
            await _roomRepository.UpdateAsync(room);

            // 3. Trả về trạng thái bàn cờ hiện tại để Khán giả có thể xem ngay
            return new RoomState
            {
                RoomId = room.RoomId,
                PlayerA = room.PlayerA,
                PlayerB = room.PlayerB,
                Status = room.Status,
                // Board và MoveHistory sẽ được map ở đây tùy theo cấu trúc JSON bạn muốn
            };
        }
    }
}
