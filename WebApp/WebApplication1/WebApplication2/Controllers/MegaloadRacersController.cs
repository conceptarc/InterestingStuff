using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LANWeb.Models.Global;
using LANWeb.Models;

namespace LANWeb.Controllers
{
    public class MegaloadRacersController : Controller
    {
        // GET: MegaloadRacers
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult HostGame(int playerCount, int mapId)
        {
            string code = GameSessionUtil.GenerateNewSessionCode();
            if (code.Length == 0) throw new Exception("Too many sessions are ongoing!");

            GameSession session = new GameSession();
            session.GameType = "MegaloadRacers";
            session.GameDescription = "Hosted on " + DateTime.Now.ToShortDateString();
            session.PlayerCount = playerCount;
            session.MapId = mapId;
            session.SessionCode = code;

            GlobalTempData.GamesInSession[code] = session;

            return PartialView("HostView", session);
        }

        [HttpPost]
        public ActionResult LoadMap(int playerCount, int mapId)
        {
            switch (mapId)
            {
                case 1:
                    // load stuff specific to each map?
                    break;
                case 2:
                    break;
                case 3:
                    break;
                default:
                    return null;
            }

            return PartialView("Map" + mapId);
        }
    }
}