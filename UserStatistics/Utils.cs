using System;
using System.IO;
using TShockAPI;

namespace UserStatistics
{
    static class Utils
    {
        public static string DatabasePath { get { return Path.Combine(TShock.SavePath, "User Statistics.sqlite"); } }
        public static string LogPath { get { return Path.Combine(TShock.SavePath, "User Statistics Log.txt"); } }

        /// <summary>
        /// Purges things from the database, as according to the config file.
        /// </summary>

        public static void Log(string info)
        {
            try
            {
                // DateTime.Now.ToString() - info
                File.AppendAllText(LogPath, "[" + DateTime.Now.ToShortTimeString() + "] " + info + Environment.NewLine);
            }
            catch (Exception ex)
            {
                TSPlayer.Server.SendErrorMessage("User statistics logging problem: " + ex.ToString());
            }
        }

        public static void InitializeLog()
        {
            if (!File.Exists(LogPath)) File.Create(LogPath); 

            File.AppendAllText(LogPath, "--|--|--|--|--|-- Beginning of log for {0} --|--|--|--|--|--"
                .SFormat(DateTime.Now.ToDisplayString())+Environment.NewLine+Environment.NewLine);
        }

        /// <summary>
        /// Returns the DateTime in SQL serialized specific form.
        /// </summary>
        public static string ToSQLString(this DateTime time)
        {
            return time.ToString("dd|MM|yy, hh:mm");
        }
        /// <summary>
        /// Creates a DateTime from serialized form in the SQL database.
        /// </summary>
        /// <param name="input">The text in the SQL table.</param>
        public static DateTime DTFromSQLString(string input)
        { // dd/MM/yy, hh:mm

            var parseComma = input.Split(',');
            // dd/mm/yy = pc[0] // hh:mm = pc[1]

            var dmy = parseComma[0].Trim().Split('|');
            // dd = 0, MM = 1, yy = 2

            var hm = parseComma[1].Trim().Split(':');
            // hh = o, mm = 1

            if (dmy[2].Trim().Length != 4) dmy[2] = "20" + dmy[2].Trim();

            int day, month, year, hour, min;

            int.TryParse(dmy[0], out day);
            int.TryParse(dmy[1], out month);
            int.TryParse(dmy[2], out year);
            int.TryParse(hm[0], out hour);
            int.TryParse(hm[1], out min);

            return new DateTime(year, month, day, hour, min, 0);
        }
        /// <summary>
        /// Returns a specific DateTime.ToString() for display purposes.
        /// </summary>
        public static string ToDisplayString(this DateTime time)
        {
            //return time.ToString(@"hh\:mm on dd/mm/yy");
            //return string.Format("{0} {1} at {3}:{4}",
            return time.ToString("MMM dd") + " at " + time.ToString("hh:mm");
                
        }
        /// <summary>
        /// Returns the TimeSpam in SQL serialized string form.
        /// </summary>
        public static string ToSqlString(this TimeSpan time)
        {
            return time.ToString(@"d\:hh\:mm\:ss"); 
        }
        /// <summary>
        /// Constructor from the SQL serialized string form.
        /// </summary>
        /// <param name="input">The string from SQL table.</param>
        public static TimeSpan TSFromSQLString(string input)
        {
            var times = input.Split(':');
            // d:hh:mm:ss
            int day, hour, min, sec;
            int.TryParse(times[0], out day);
            int.TryParse(times[1], out hour);
            int.TryParse(times[2], out min);
            int.TryParse(times[3], out sec);

            return new TimeSpan(day, hour, min, sec);
        }
        /// <summary>
        /// A TimeSpam.ToString() specifically for plugin's display purposes.
        /// </summary>
        public static string ToDisplayString(this TimeSpan time)
        {
            return time.ToString("dd days, hh hours and m minutes");
        }
    }
}
