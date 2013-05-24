using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using DotLiquid;
using LazerTagHostLibrary;

namespace LazerTagHostUI
{
	public partial class ScoreReport : Form
	{
		public ScoreReport()
		{
			InitializeComponent();
		}

		public ScoreReport(HostGun hostGun)
		{
			InitializeComponent();
			_hostGun = hostGun;
		}

		private readonly HostGun _hostGun;

		private void ScoreReport_Load(object sender, EventArgs e)
		{
			string contentTemplate = File.ReadAllText(@"html\ScoreReport.html");
			var template = Template.Parse(contentTemplate);
			Template.RegisterFilter(typeof (OrdinalFilter));
			Template.RegisterFilter(typeof (YesNoFilter));
			Template.RegisterSafeType(typeof (Team), new[] {"TeamNumber", "TeamRank"});
			Template.RegisterSafeType(typeof (Player), new[] {"TeamPlayerId", "PlayerName", "Rank", "Score", "Survived", "TaggedByPlayerCounts"});
			Template.RegisterSafeType(typeof (TeamPlayerId), new[] {"PlayerNumber", "TeamNumber", "TeamPlayerNumber"});

			var executableDirectory = Path.GetDirectoryName(Application.ExecutablePath);
			if (executableDirectory == null) throw new Exception("Could not determine the name of the directory in which this executable is located.");
			var basePath = Path.Combine(executableDirectory, "html");

			//const bool testIsTeamGame = true;
			//var testTeams = new TeamCollection()
			//    {
			//        new Team(1)
			//            {
			//                TeamRank = 2,
			//            },
			//        new Team(2)
			//            {
			//                TeamRank = 1,
			//            },
			//        new Team(3)
			//            {
			//                TeamRank = 3,
			//            },
			//    };
			//var testPlayers = new List<Player>
			//    {
			//        new Player(0)
			//            {
			//                TeamPlayerId = new TeamPlayerId(1,1),
			//                PlayerName = "Alpha",
			//                Rank = 4,
			//                Score = 1,
			//                Survived = true,
			//                TaggedByPlayerCounts = new []{0,5,0,0,0,0,0,0,10,15,0,0,0,0,0,0,20,0,0,0,0,0,0,0},
			//            },
			//        new Player(1)
			//            {
			//                TeamPlayerId = new TeamPlayerId(1,2),
			//                PlayerName = "Beta",
			//                Rank = 1,
			//                Score = 4,
			//                Survived = true,
			//                TaggedByPlayerCounts = new []{1,0,0,0,0,0,0,0,3,5,0,0,0,0,0,0,7,0,0,0,0,0,0,0},
			//            },
			//        new Player(2)
			//            {
			//                TeamPlayerId = new TeamPlayerId(2,1),
			//                PlayerName = "Gamma",
			//                Rank = 3,
			//                Score = -10,
			//                Survived = true,
			//                TaggedByPlayerCounts = new []{2,4,0,0,0,0,0,0,0,6,0,0,0,0,0,0,8,0,0,0,0,0,0,0},
			//            },
			//        new Player(3)
			//            {
			//                TeamPlayerId = new TeamPlayerId(2,2),
			//                //player_name = "Delta",
			//                Rank = 5,
			//                Score = 200,
			//                Survived = false,
			//                TaggedByPlayerCounts = new []{99,98,0,0,0,0,0,0,97,0,0,0,0,0,0,0,96,0,0,0,0,0,0,0},
			//            },
			//        new Player(4)
			//            {
			//                TeamPlayerId = new TeamPlayerId(3,1),
			//                PlayerName = "",
			//                Rank = 2,
			//                Score = 100,
			//                Survived = false,
			//                TaggedByPlayerCounts = new []{1,2,0,0,0,0,0,0,40,50,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
			//            },
			//    };
			
			var content = template.Render(Hash.FromAnonymousObject(new
				{
					base_uri = basePath,
					is_team_game = _hostGun.IsTeamGame(),
					teams = _hostGun.Teams,
					players = _hostGun.Players.Values,
					//is_team_game = testIsTeamGame,
					//teams = testTeams,
					//players = testPlayers,
				}));
			webBrowser.DocumentText = content;
			textBox.Text = content;
		}

		private void ScoreReport_FormClosed(object sender, FormClosedEventArgs e)
		{
			//Environment.Exit(0);
		}

		public static class YesNoFilter 
		{
			public static string YesNo(bool value)
			{
				return value ? "Yes" : "No";
			}
		}
	}
}
