using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeTrackingApp
{
    public static class SharedCommon
    {
        public static string GetFileName(DateTime date)
        {
            // No database for now; we will just store data into a text file (1 file per day)
            string suffix = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string fileName = $"TimeData_{suffix}.dat";

            return fileName;
        }

        public static List<TimeEntry> LoadTimeEntries(DateTime date)
        {
            // No database for now; we will just store data into a text file (1 file per day)
            string fileName = GetFileName(date);

            if (!File.Exists(fileName))
                return new List<TimeEntry>();

            try
            {
                string fileJson = File.ReadAllText(fileName);
                var entries = JsonConvert.DeserializeObject<List<TimeEntry>>(fileJson);

                return entries ?? new List<TimeEntry>();
            }
            catch
            {
                return new List<TimeEntry>();
            }
        }
    }
}
