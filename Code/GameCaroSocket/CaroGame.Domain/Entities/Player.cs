using CaroGame.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Domain.Entities
{
    public sealed class Player
    {
        public Guid PlayerId { get; }
        public string Nickname { get; private set; }

        public PlayerStats Stats { get; }

        public Player(string nickname)
        {
            PlayerId = Guid.NewGuid();
            Nickname = nickname;

            Stats = new PlayerStats();
        }

        public void ChangeNickname(string nickname)
        {
            Nickname = nickname;
        }
    }
    public sealed class PlayerStats
    {
        public int Wins { get; private set; }
        public int Losses { get; private set; }
        public int Draws { get; private set; }

        public void RecordWin() => Wins++;
        public void RecordLoss() => Losses++;
        public void RecordDraw() => Draws++;
    }

}
