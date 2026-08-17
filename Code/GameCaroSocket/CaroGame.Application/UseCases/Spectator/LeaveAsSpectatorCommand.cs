using MediatR;
using System;

namespace CaroGame.Application.UseCases.Spectator
{
    // Lệnh yêu cầu thoát khỏi phòng dành cho Khán giả
    public class LeaveAsSpectatorCommand : IRequest<bool>
    {
        public Guid RoomId { get; set; }
        public Guid UserId { get; set; }
    }
}
