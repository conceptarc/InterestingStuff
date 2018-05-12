using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System.Threading;

namespace LANWeb
{
    [HubName("testHub")]
    public class SignalrHub : Hub
    {
        public void Send(int x, int y)
        {
            Clients.Others.broadcast(x, y);
        }
    }

    [HubName("mglrHub")]
    public class MegaloadRacersHub : Hub
    {
        // 1) host loads the big map (with session code)
        // 2) player will go to the Join-a-game page to enter a session code
        // 3) if the session code is valid, the server will provide details on
        //      - which map to load
        //      - locations of each player
        //      - (for now we don't support player customization)
        // 4) after all players load, the server will be notified and countdown begins
        // 5) each player will have their controls be displayed on the screen
        // 6) each player will send their commands to the host where the results are gathered


        // From host to players. Host controls player locations and players will send key presses.
        public void BroadcastPosition(string sessionCode, int playerId, int x, int y)
        {
            Clients.Others.broadcast(sessionCode, playerId, x, y);
        }

        public void RefreshPlayers(string sessionCode)
        {
            Clients.All.update(sessionCode);
        }

        // periodic polling of data
        public void UpdatePlayerControls(string sessionCode, int playerId, int accel, int turn)
        {
            Clients.Others.updatePlayer(sessionCode, playerId, accel, turn);
        }

        // use this method to send key interrupts from the player
        // ready, pause, use items (in game), misc
        public void SendPlayerKey(string sessionCode, int playerId, string key)
        {
            Clients.Others.sendPlayerKey(sessionCode, playerId, key);
        }
    }
}