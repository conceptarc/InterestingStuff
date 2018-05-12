using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LANWeb.Models
{
    public class GameSession
    {
        public string GameType { get; set; }
        public string GameDescription { get; set; }
        public string SessionCode { get; set; }
        public int MaxPlayers { get; set; }
        public int MapId { get; set; }

        private List<Player> _playerList;
        public List<Player> PlayerList
        {
            get
            {
                if (_playerList == null)
                    _playerList = new List<Player>();
                return _playerList;
            }
        }
    }
}