namespace CaroGame.Shared.Networking.Messaging
{
    public enum MessageTypes : byte
    {
        //Lobby
        OnlinePlayersListResponse = 1,       // xử lý bởi OnlinePlayerFinder
        PlayerOnlineNotification = 2,        // phát ra bởi PlayerJoiner (Lobby)
        PlayerOfflineNotification = 3,
        PlayerStatusChangedNotification = 4,
        ActiveMatchesListRequest = 5,        // xử lý bởi OngoingMatchFinder
        ActiveMatchesListResponse = 6,

        // ===== MatchMaking (300-399) — TCP =====
        ChallengeRequest = 21,                // xử lý bởi ChallengeSender
        ChallengeReceivedNotification = 22,
        ChallengeResponse = 23,               // xử lý bởi ChallengeResponder (bao gồm cả accept/decline/cancel/expire)
        ChallengeAcceptedNotification = 24,
        ChallengeDeclinedNotification = 25,
        ChallengeCancelRequest = 26,
        ChallengeExpiredNotification = 27,

        // ===== Match (400-499) — TCP =====
        MatchStartedNotification = 41,        // phát ra sau khi ChallengeResponder tạo phòng
        JoinRoomAsSpectatorRequest = 42,      // xử lý bởi SpectatorJoiner
        JoinRoomAsSpectatorResponse = 43,
        SpectatorJoinedNotification = 44,
        LeaveRoomRequest = 45,                // CHỈ dành cho khán giả, xử lý bởi SpectatorLeaver
        SpectatorLeftNotification = 46,

        // ===== GamePlay (500-599) =====
        MoveRequest = 61,                     // TCP - xử lý bởi MoveSubmitter
        MoveRejected = 62,                    // TCP
        MoveBroadcastNotification = 63,       // TCP
        TurnTimerUpdateNotification = 64,     // UDP - mất gói không sao, tick sau tự sửa
        TurnTimeoutNotification = 65,         // TCP - phát ra bởi TurnTimeoutHandler
        SurrenderRequest = 66,                // TCP - xử lý bởi MatchEnder
        GameOverNotification = 67,            // TCP - phát ra bởi MatchEnder (mọi lý do: FiveInRow/Timeout/Surrender/OpponentDisconnectTimeout)
        BoardStateSnapshotResponse = 68,      // TCP - dùng khi SpectatorJoiner hoặc PlayerReconnector cần gửi full trạng thái
    }
}
