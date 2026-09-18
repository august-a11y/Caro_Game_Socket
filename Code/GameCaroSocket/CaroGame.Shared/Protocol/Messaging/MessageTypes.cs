namespace CaroGame.Shared.Networking.Messaging
{
    public enum MessageTypes : byte
    {
        // Lobby (1-19) — TCP
        OnlinePlayersListResponse = 1,       // xử lý bởi OnlinePlayerFinder
        PlayerOnlineNotification = 2,        // phát ra bởi PlayerJoiner (Lobby)
        PlayerOfflineNotification = 3,
        PlayerStatusChangedNotification = 4,
        ActiveMatchesListRequest = 5,        // xử lý bởi OngoingMatchFinder
        ActiveMatchesListResponse = 6,
        OnlinePlayersListRequest = 7,        // Client yêu cầu tải lại danh sách online

        // Challenge (20-39) — TCP
        ChallengeRequest = 21,                // xử lý bởi ChallengeSender
        ChallengeReceivedNotification = 22,
        ChallengeRespondRequest = 23,        // Client chấp nhận/từ chối, xử lý bởi ChallengeResponder
        ChallengeAcceptedNotification = 24,
        ChallengeDeclinedNotification = 25,
        ChallengeCancelRequest = 26,         // xử lý bởi ChallengeCanceller, chỉ người gửi được hủy
        ChallengeExpiredNotification = 27,
        ChallengeSendResponse = 28,          // Xác nhận kết quả gửi lời mời cho người gửi
        ChallengeCancelledNotification = 29, // Thông báo lời mời đã bị hủy

        // Match (40-59) — TCP
        PlayerReadyRequest = 41,             // xử lý bởi PlayerReadyHandler
        MatchStartedNotification = 42,       // Server phát khi cả hai ready và trận đã bắt đầu
        JoinRoomAsSpectatorRequest = 43,      // xử lý bởi SpectatorJoiner
        JoinRoomAsSpectatorResponse = 44,
        SpectatorJoinedNotification = 45,
        LeaveRoomAsSpectatorRequest = 46,     // xử lý bởi SpectatorLeaver
        SpectatorLeftNotification = 47,
        PlayerReadyNotification = 48,        // Thông báo người chơi đã sẵn sàng
        MatchPausedNotification = 49,        // Kèm người mất kết nối và hạn reconnect
        MatchResumedNotification = 50,       // Kèm lượt hiện tại và deadline mới

        LeaveWaitingRoomRequest = 51,
        RoomCancelledNotification = 52,
        WaitingRoomUpdatedNotification = 53,
        RematchOfferNotification = 54,
        RematchResponseRequest = 55,
        RematchResponseNotification = 56,

        // Gameplay (60-69)
        MoveRequest = 61,                     // TCP - xử lý bởi MoveSubmitter
        MoveRejected = 62,                    // TCP
        MoveBroadcastNotification = 63,       // TCP
        TurnTimerUpdateNotification = 64,     // UDP - mất gói không sao, tick sau tự sửa
        TurnTimeoutNotification = 65,         // TCP - phát ra bởi TurnTimeoutHandler
        SurrenderRequest = 66,                // TCP - cần xác thực người đầu hàng trước khi gọi MatchEnder
        GameOverNotification = 67,            // TCP - phát ra bởi MatchEnder (mọi lý do: FiveInRow/Timeout/Surrender/OpponentDisconnectTimeout)
        BoardStateSnapshotResponse = 68,      // TCP - dùng khi SpectatorJoiner hoặc PlayerReconnector cần gửi full trạng thái
        BoardStateSnapshotRequest = 69,       // TCP - Client chủ động yêu cầu đồng bộ lại

        // Session (80-99) — TCP
        PlayerJoinRequest = 81,               // Client gửi nickname, xử lý bởi PlayerJoiner
        PlayerJoinResponse = 82,              // Trả kết quả join, player ID và session ID
        PlayerReconnectRequest = 83,          // Gửi phiên cũ, xử lý bởi PlayerReconnector
        PlayerReconnectResponse = 84,         // Trả kết quả reconnect và phòng cần khôi phục
        Heartbeat = 85,                       // Client → Server, xử lý bởi SessionHeartbeatHandler
        HeartbeatResponse = 86,               // Server → Client, xác nhận heartbeat và hỗ trợ đo RTT
        RegisterUdpEndpointRequest = 87,      // Đăng ký endpoint nhận UDP, xử lý bởi UdpEndpointRegistrar
        RegisterUdpEndpointResponse = 88,     // Xác nhận kết quả đăng ký endpoint

        // Common — TCP
        ErrorResponse = 255,                  // Lỗi chung, payload gồm RequestId, ErrorCode và Message
    }
}
