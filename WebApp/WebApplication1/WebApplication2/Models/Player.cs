using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LANWeb.Models
{
    public class Player
    {
        public int PlayerId { get; set; }
        public string Name { get; set; }
        public int Score { get; set; }
        public int WinCount { get; set; }
        public int LoseCount { get; set; }

    }
}