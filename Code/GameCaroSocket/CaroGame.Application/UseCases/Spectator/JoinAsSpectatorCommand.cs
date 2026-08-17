using CaroGame.Application.Contracts;
using MediatR;
using System;

namespace CaroGame.Application.UseCases.Spectator
{
    // Yêu cầu xin vào xem trận đấu, trả về toàn bộ RoomState để vẽ bàn cờ
    public class JoinAsSpectatorCommand : IRequest<RoomState?>
    {
        public Guid RoomId { get; set; }
        public Guid UserId { get; set; }
    }
}
