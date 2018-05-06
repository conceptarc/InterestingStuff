using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LANWeb.Models.Global;

namespace LANWeb.Models
{
    public static class GameSessionUtil
    {
        public static string GenerateNewSessionCode(int length = 5, int maxTries = 10)
        {
            /* E.g. 00AA will not conflict with 00AA0
             * 1111 will not conflict with 111 nor 11 nor 11111
             */

            List<string> existingCodes = GlobalTempData.GamesInSession.Keys
                .Where(m => m.Length == length).ToList(); // possible conflicts with same length

            // You know what, let's just randomize the alphanumeric characters until
            // we start to find a conflict.

            string validChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            double totalPossibilities = Math.Pow(36, length);
            double chanceOfConflict = existingCodes.Count / totalPossibilities;

            if (chanceOfConflict > 0.1) return string.Empty;

            Random rand = new Random();
            for (int i = 0; i < maxTries; i++)
            {
                string output = "";
                for (int j = 0; j < length; j++)
                {
                    output += validChars[rand.Next(0, 36)];
                }
                if (!existingCodes.Contains(output)) return output;
            }

            return string.Empty;
        }
    }
}