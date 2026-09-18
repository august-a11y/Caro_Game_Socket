namespace Caro.Client.WinForms.Core;

internal static class UiText
{
    public static string RoomStatus(string? status) => status switch
    {
        "Waiting" => "Đang chờ sẵn sàng",
        "Playing" => "Đang chơi",
        "Finished" => "Đã kết thúc",
        "Cancelled" => "Đã hủy",
        "Paused" => "Tạm dừng",
        null or "" => "Chưa xác định",
        _ => status
    };

    public static string ChallengeStatus(string? status) => status switch
    {
        "Pending" => "Đang chờ phản hồi",
        "Accepted" => "Đã chấp nhận",
        "Rejected" => "Đã từ chối",
        "Cancelled" => "Đã hủy",
        "Expired" => "Đã hết hạn",
        null or "" => "Chưa xác định",
        _ => status
    };

    public static string MatchResult(string? result) => result switch
    {
        "PlayerXWin" => "Người chơi X thắng",
        "PlayerOWin" => "Người chơi O thắng",
        "Draw" => "Hòa",
        "Continue" => "Đang diễn ra",
        null or "" => "Chưa xác định",
        _ => result
    };

    public static string ClosingReason(string? reason) => reason switch
    {
        "FiveInRow" or "FiveInARow" => "Có năm quân liên tiếp",
        "Draw" => "Ván đấu hòa",
        "Surrender" => "Một người chơi đầu hàng",
        "Timeout" or "TimeOut" => "Hết thời gian lượt đi",
        "OpponentDisconnectTimeout" or "DisconnectForfeit" =>
            "Người chơi mất kết nối và không quay lại đúng hạn",
        "ReadyTimeout" => "Hết thời gian chờ sẵn sàng",
        "PlayerLeft" => "Một người chơi đã rời phòng",
        null or "" => "Không có thông tin",
        _ => reason
    };
}
