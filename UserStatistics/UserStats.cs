using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace UserStatistics
{
    [ApiVersion(2, 1)]
    public class UserStatistics : TerrariaPlugin
    {

        private DateTime LastRefresh = DateTime.Now;
        private DateTime LastUpdate = DateTime.Now;
        private StatPlayer[] StatPlayers;

        public UserStatistics(Main game) : base(game) 
        { 
            Order = 2; 
        }
        public override string Name { get { return "User Statistics"; } }

        public override string Author { get { return "Snirk Immington, rewritten by Nova4334"; } }

        public override string Description { get { return "Useful playercount tracker"; } }

        public override Version Version { get { return new Version(2,0); } }

        public override void Initialize()
        {
            StatPlayers = new StatPlayer[255];

            Database.SetupDB();
            Utils.InitializeLog();

            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGr);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
 
            Commands.ChatCommands.Add(new Command(
                "userstats.userstats", 
                UserInfo, 
                "userstats", 
                "us"));

            AppDomain.CurrentDomain.UnhandledException += OnFail;
        }
        private void OnFail(object e, UnhandledExceptionEventArgs a)
        {
            if (a.IsTerminating) SaveDatabase(); 
        }

        private void OnGr(GreetPlayerEventArgs e)
        {
            StatPlayers[e.Who] = new StatPlayer(TShock.Players[e.Who]);
            // Login and stuff handled in constructor.
        }
        private void OnLeave(LeaveEventArgs e)
        {
            // Dayum, that exception happened.
            if (StatPlayers[e.Who] != null) StatPlayers[e.Who].LogOut();
            StatPlayers[e.Who] = null;
        }
        private void OnUpdate(EventArgs args)
        {

            if ((DateTime.Now - LastRefresh).Seconds >= 2)
            { LastRefresh = DateTime.Now;

                for (int i = 0; i < 255; i++)
                {

                    if (StatPlayers[i] != null && TShock.Players[i].RealPlayer)
                    {
                        // Check that login has changed
                        if (StatPlayers[i].ExpectedID.ToString() != TShock.Players[i].UUID) 
                        {
                            // Log the player in.
                            if (StatPlayers[i].ExpectedID != -1) StatPlayers[i].LogOut();

                            StatPlayers[i].LogIn();
                            Utils.Log("Logged in user {0} to statistics database".SFormat(TShock.Players[i].Name));
                        }
                    }
                }
            }

            if ((DateTime.Now - LastUpdate).TotalMinutes >= 5)
            {
                LastUpdate = DateTime.Now;

                for (int i = 0; i < 255; i++)
                {
                    if (StatPlayers[i] != null && StatPlayers[i].ExpectedID.ToString() == TShock.Players[i].UUID && TShock.Players[i].IsLoggedIn)
                        StatPlayers[i].UpdateSession();
                }
            }
        }

        public void UserInfo(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Usage: /userstats (/us) <player> - gets the User Statistics data of the player's account!"); return;
            }

            var ply = TSPlayer.FindByNameOrID(string.Join(" ", args.Parameters));

            if (ply.Count != 1) { args.Player.SendErrorMessage(ply.Count + " players matched!"); return; }

            if (!ply[0].IsLoggedIn) { args.Player.SendErrorMessage(ply[0].Name + " is not logged in."); return; }

            var dat = StatPlayers[ply[0].Index];

            args.Player.SendInfoMessage("Account statistics for {0}:".SFormat(ply[0].Name));
            args.Player.SendSuccessMessage("Registered {0} | Member for {1}".SFormat(dat.StatInfo.RegisterTime.ToDisplayString(), dat.StatInfo.TotalTime.ToDisplayString()));
        }
        public void SelfInfoCom(CommandArgs args)
        {
            var dat = StatPlayers[args.Player.Index];
            args.Player.SendInfoMessage("Account statistics for {0}:".SFormat(args.Player.Name));
            args.Player.SendSuccessMessage("Registered {0} | Member for {1}".SFormat(dat.StatInfo.RegisterTime.ToDisplayString(), dat.StatInfo.TotalTime.ToDisplayString()));
        }

        public void SaveDatabase()
        {
            for (int i = 0; i < 255; i++)
            {
                if (StatPlayers[i] != null && StatPlayers[i].ExpectedID != -1)
                {
                    StatPlayers[i].LogOut();
                }
                
            }
        }
    }

    /// <summary>
    /// Contains plugin-unique information.
    /// </summary>
    public class StatPlayer
    {
        /// <summary>
        /// The index of the player.
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// The last known user id of the player.
        /// </summary>
        public int ExpectedID { get; set; }
        /// <summary>
        /// The database-stored info based on userid.
        /// </summary>
        internal DBInfo StatInfo { get; set; }
        /// <summary>
        /// Used to see last login time as well.
        /// </summary>
        public DateTime SessionLogin { get; set; }
        /// <summary>
        /// Gets the time of the last check for updating user time.
        /// </summary>
        public DateTime LastCheck { get; set; }

        /// <summary>
        /// Constructor for onjoin.
        /// </summary>
        public StatPlayer(TSPlayer ply)
        {
            Index = ply.Index;
            ExpectedID.ToString(ply.UUID);
            if (ply.IsLoggedIn) LogIn();
        }

        /// <summary>
        /// Registers the player's DBInfo with the database and sets up login timing.
        /// </summary>
        public void LogIn()
        {
            ExpectedID.ToString(TShock.Players[Index].UUID);
            StatInfo = Database.GetPlayerInfo(ExpectedID);
            StatInfo.LastLogin = DateTime.Now;
            LastCheck = DateTime.Now;
            Database.UpdateSQL(StatInfo);
        }
        /// <summary>
        /// Gets logged in time, udpates the DB object.
        /// </summary>
        public void LogOut()
        {
            StatInfo.TotalTime += StatInfo.LastLogin - DateTime.Now;
            Database.UpdateSQL(StatInfo);
        }

        public void UpdateSession()
        {
            StatInfo.TotalTime += DateTime.Now - LastCheck;
            LastCheck = DateTime.Now;

            Database.UpdateSQL(StatInfo);
        }
    }
}
