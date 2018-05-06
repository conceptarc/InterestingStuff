using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LANWeb.Models.Global_Private;

namespace LANWeb.Models.Global_Private
{
    static class _globalTempData
    {
        public static Dictionary<string, GameSession> _gamesInSession { get; set; }
    }
}

namespace LANWeb.Models.Global
{
    public static class GlobalTempData
    {
        // Wrap this other object and help manage initiation of the dictionary.
        public static Dictionary<string, GameSession> GamesInSession
        {
            get
            {
                if (_globalTempData._gamesInSession == null)
                    _globalTempData._gamesInSession = new Dictionary<string, GameSession>();
                return _globalTempData._gamesInSession;
            }
            set
            {
                _globalTempData._gamesInSession = value;
            }
        }
    }
}