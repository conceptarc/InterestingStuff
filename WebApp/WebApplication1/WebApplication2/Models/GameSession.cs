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
        public int PlayerCount { get; set; }
        public int MapId { get; set; }
    }
}