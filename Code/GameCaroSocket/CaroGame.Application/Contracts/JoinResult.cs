using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.Contracts
{
    public sealed record Session
    (
        string UserId,
        string SessionToken,
        PlayerInfo PlayerInfo
    );
}
