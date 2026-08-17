using CaroGame.Application.Contracts;
using MediatR;
using System.Collections.Generic;

namespace CaroGame.Application.UseCases.Spectator
{
    // Truy vấn lấy danh sách các phòng đang đánh để khán giả chọn vào xem
    public class GetOngoingRoomsQuery : IRequest<List<RoomState>>
    {
        // Có thể thêm phân trang (Page, PageSize) ở đây nếu cần sau này
    }
}
