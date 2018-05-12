using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LANWeb.Models.Global;
using LANWeb.Models;
using Newtonsoft.Json;

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
            session.MaxPlayers = playerCount;
            session.MapId = mapId;
            session.SessionCode = code;

            GlobalTempData.GamesInSession[code] = session;

            return PartialView("HostView", session);
        }

        // this one can be refactored to a generic base class
        //[HttpPost, HttpGet]
        public ActionResult LoadMap(string sessionCode, int? playerId = null)
        {
            GameSession session = GlobalTempData.GamesInSession[sessionCode];
            List<MglrRoadSection> roadSections = new List<MglrRoadSection>();
            List<List<MglrRoadBorderLine>> roadLines = new List<List<MglrRoadBorderLine>>();

            switch (session.MapId)
            {
                // load stuff specific to each map?
                case 1:
                    #region defining the map
                    int radius = 22;
                    var roadLines1 = new MglrRoadBorderManager(); // outer lines
                    var roadLines2 = new MglrRoadBorderManager(); // inner lines

                    roadSections.Add(new MglrRoadSection() { x = 200, y = 100, r = radius });
                    roadLines1.AddLineNode(roadSections.Last(), 0, -radius - 5);
                    roadLines2.AddLineNode(roadSections.Last(), radius, radius + 25);

                    roadSections.Add(new MglrRoadSection() { x = 570, y = 270, r = radius });
                    roadLines1.AddLineNode(roadSections.Last(), 5, -radius - 5);
                    roadLines2.AddLineNode(roadSections.Last(), -5, radius + 15);
                    roadLines2.AddLineNode(roadSections.Last(), radius, radius + 15);

                    roadSections.Add(new MglrRoadSection() { x = 790, y = 100, r = radius });
                    roadLines1.AddLineNode(roadSections.Last(), 5, -radius - 5);
                    roadLines2.AddLineNode(roadSections.Last(), radius - 5, radius + 25);

                    roadSections.Add(new MglrRoadSection() { x = 990, y = 200, r = radius });
                    roadLines1.AddLineNode(roadSections.Last(), radius, -radius);
                    roadLines2.AddLineNode(roadSections.Last(), -radius, radius + 15);

                    // right-most point
                    roadSections.Add(new MglrRoadSection() { x = 1100, y = 400, r = radius });
                    roadLines1.AddLineNode(roadSections.Last(), radius + 15, 0);
                    roadLines1.AddLineNode(roadSections.Last(), radius + 15, radius);
                    roadLines2.AddLineNode(roadSections.Last(), -radius - 5, 0);

                    roadSections.Add(new MglrRoadSection() { x = 900, y = 600, r = radius });
                    roadLines1.AddLineNode(roadSections.Last(), radius + 5, radius + 25);
                    roadLines2.AddLineNode(roadSections.Last(), -5, -radius - 5);

                    roadSections.Add(new MglrRoadSection() { x = 800, y = 600, r = radius });
                    roadLines1.AddLineNode(roadSections.Last(), -radius + 5, radius + 25);
                    roadLines2.AddLineNode(roadSections.Last(), radius, -radius - 5);

                    roadSections.Add(new MglrRoadSection() { x = 600, y = 400, r = radius });
                    roadLines1.AddLineNode(roadSections.Last(), -radius, radius + 5);
                    roadLines2.AddLineNode(roadSections.Last(), radius + 5, -radius - 5);

                    // bottom peak
                    roadSections.Add(new MglrRoadSection() { x = 400, y = 300, r = radius });
                    roadLines1.AddLineNode(roadSections.Last(), radius - 5, radius + 25);
                    roadLines2.AddLineNode(roadSections.Last(), radius - 5, -radius - 5);
                    roadLines2.AddLineNode(roadSections.Last(), 0, -radius - 5);

                    roadSections.Add(new MglrRoadSection() { x = 120, y = 600, r = radius });
                    roadLines1.AddLineNode(roadSections.Last(), radius, radius + 25);
                    roadLines1.AddLineNode(roadSections.Last(), -radius, radius + 25);
                    roadLines2.AddLineNode(roadSections.Last(), radius - 5, -radius - 15);

                    roadSections.Add(new MglrRoadSection() { x = 90, y = 560, r = radius });
                    roadLines1.AddLineNode(roadSections.Last(), -radius, radius);

                    roadSections.Add(new MglrRoadSection() { x = 140, y = 200, r = radius });
                    roadLines1.AddLineNode(roadSections.Last(), -radius, -5);
                    roadLines2.AddLineNode(roadSections.Last(), radius + 25, 0);

                    roadLines.Add(roadLines1.GenerateLines());
                    roadLines.Add(roadLines2.GenerateLines());

                    #endregion
                    break;
                case 2:
                    break;
                case 3:
                    break;
                default:
                    return null;
            }
            
            ViewBag.GameSession = session;
            ViewBag.MglrRoadSections = InterpolateRoad(roadSections);
            ViewBag.MglrRoadLines = roadLines;

            if (playerId.HasValue)
                ViewBag.Player = session.PlayerList.Where(m => m.PlayerId == playerId.Value).First();

            return PartialView("~/Views/" + session.GameType + "/Map" + session.MapId + ".cshtml");
        }

        // this one can be refactored to a generic base class
        [HttpPost]
        public string GetSessionInfo(string sessionCode)
        {
            if (!GlobalTempData.GamesInSession.ContainsKey(sessionCode)) return "";

            var session = GlobalTempData.GamesInSession[sessionCode];

            return JsonConvert.SerializeObject(session);
        }

        private List<MglrRoadSection> InterpolateRoad(List<MglrRoadSection> roadSkeleton)
        {
            // assume roadSkeleton has at least 3 elements
            List<MglrRoadSection> result = new List<MglrRoadSection>();

            for (int i = 1; i < roadSkeleton.Count; i++)
            {
                var startPoint = roadSkeleton[i - 1];
                var endPoint = roadSkeleton[i];
                result.AddRange(InterpolateTwo(startPoint, endPoint));
            }
            // complete the loop from last node to first node
            result.AddRange(InterpolateTwo(roadSkeleton[roadSkeleton.Count - 1], roadSkeleton[0]));

            return result;
        }

        private List<MglrRoadSection> InterpolateTwo(MglrRoadSection first, MglrRoadSection second)
        {
            List<MglrRoadSection> result = new List<MglrRoadSection>();
            int dy = second.y - first.y;
            int dx = second.x - first.x;
            double dist = Math.Sqrt(dy * dy + dx * dx);
            // assume the radius of the road-circle block is always non zero
            int width = second.r * 2;
            int blockNumber = (int)Math.Ceiling(dist / width);
            for (int j = 0; j < blockNumber; j++)
            {
                double deltaX = (double)dx / blockNumber;
                double deltaY = (double)dy / blockNumber;
                result.Add(new MglrRoadSection()
                {
                    r = second.r,
                    x = (int)(first.x + deltaX * j),
                    y = (int)(first.y + deltaY * j)
                });
            }
            return result;
        }
    }

    public class MglrRoadSection
    {
        public int x { get; set; }
        public int y { get; set; }
        public int r { get; set; }
    }

    public class MglrRoadBorderLine
    {
        public int x1 { get; set; }
        public int y1 { get; set; }
        public int x2 { get; set; }
        public int y2 { get; set; }
    }

    public class MglrRoadBorderManager
    {
        private List<MglrRoadBorderLine> keyNodes;

        public MglrRoadBorderManager()
        {
            keyNodes = new List<MglrRoadBorderLine>();
        }

        // this should make it easier to draw the road borders
        // by making it relative to a road secion
        public void AddLineNode(MglrRoadSection reference, int xOff, int yOff)
        {
            int newX = reference.x + xOff;
            int newY = reference.y + yOff;
            if (keyNodes.Count == 0)
            {
                keyNodes.Add(new MglrRoadBorderLine() { x1 = newX, y1 = newY });
            }
            else
            {
                var lastLine = keyNodes.Last();
                lastLine.x2 = newX;
                lastLine.y2 = newY;
                keyNodes.Add(new MglrRoadBorderLine() { x1 = newX, y1 = newY });
            }
        }

        public List<MglrRoadBorderLine> GenerateLines()
        {
            if (keyNodes.Count == 0)
                return keyNodes;

            // complete the loop - connect the lines
            keyNodes.Last().x2 = keyNodes.First().x1;
            keyNodes.Last().y2 = keyNodes.First().y1;
            return keyNodes;
        }
    }
}