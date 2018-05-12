using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LANWeb.Models.Global;
using LANWeb.Models;

namespace LANWeb.Controllers
{
    public class JoinGameController : Controller
    {
        // GET: JoinGame
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult JoinRoom(string sessionCode, string name)
        {
            sessionCode = (sessionCode ?? "").ToUpper();

            if (!GlobalTempData.GamesInSession.ContainsKey(sessionCode))
                throw new Exception("this message doesn't matter");

            var game = GlobalTempData.GamesInSession[sessionCode];
            Player player = game.PlayerList.Where(m => m.Name == name).FirstOrDefault();

            if (player == null) // new player
            {
                if (game.PlayerList.Count >= game.MaxPlayers)
                    throw new Exception("this message doesn't matter - for now");

                player = new Player();
                player.Name = name;
                player.PlayerId = game.PlayerList.Count + 1;

                GlobalTempData.GamesInSession[sessionCode].PlayerList.Add(player);
            }
            
            return RedirectToAction("/LoadMap", game.GameType,
                new { sessionCode = game.SessionCode, playerId = player.PlayerId });

            //return PartialView("~/Views/" + game.GameType + "/Map" + game.MapId + ".cshtml");
        }
    }
}