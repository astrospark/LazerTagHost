using System;
using System.Windows.Forms;
using LazerTagHostLibrary;

namespace LazerTagHostUI
{
	public partial class ScoreReport : Form
	{
		public ScoreReport(HostGun hostGun)
		{
			InitializeComponent();
			_hostGun = hostGun;
		}

		private readonly HostGun _hostGun;

		private void ScoreReport_Load(object sender, EventArgs e)
		{
			if (_hostGun == null || _hostGun.GetGameState() != HostGun.HostingState.HOSTING_STATE_GAME_OVER) return;

			Clear();

			var line = string.Empty;

			// Team Rankings
			if (_hostGun.IsTeamGame())
			{
				WriteLine("Team Rankings");
				WriteLine();

				foreach (var team in _hostGun.Teams)
				{
					line += string.Format("Team {0}\t", team.TeamNumber);
					line += Ordinal.FromCardinal(team.TeamRank);
				}
				WriteLine(line);
				WriteLine();
				WriteLine();
			}

			line = string.Empty;
			line += "Player\t";
			line += "Rank\t";
			line += "Score\t";
			line += "Survived\t";
			for (var teamNumber = 1; teamNumber <= 3; teamNumber++)
			{
				for (var playerNumber = 1; playerNumber <= 8; playerNumber++)
				{
					var player = _hostGun.LookupPlayer(teamNumber, playerNumber);
					if (player == null || string.IsNullOrWhiteSpace(player.player_name))
					{
						line += string.Format("{0}:{1:d2}\t", teamNumber, playerNumber);
					}
					else
					{
						line += player.player_name + "\t";
					}
				}
			}
			WriteLine(line);

			for (var teamNumber = 1; teamNumber <= 3; teamNumber++)
			{
				for (var playerNumber = 1; playerNumber <= 8; playerNumber++)
				{
					line = string.Empty;
					var player = _hostGun.LookupPlayer(teamNumber, playerNumber);
					if (player == null)
					{
						line += string.Format("{0}:{1:d2}\t", teamNumber, playerNumber);
						line += "-\t"; // Rank
						line += "-\t"; // Score
						line += "-\t"; // Survived
						for (int i = 0; i < 24; i++)
						{
							line += "-\t";
						}
					}
					else
					{
						if (string.IsNullOrWhiteSpace(player.player_name))
						{
							line += string.Format("{0}:{1:d2}\t", teamNumber, playerNumber);
						}
						else
						{
							line += player.player_name + "\t";
						}

						line += Ordinal.FromCardinal(player.individual_rank) + "\t";
						line += player.score + "\t";
						line += player.alive + "\t";
						for (var teamIndex = 0; teamIndex < 3; teamIndex++)
						{
							for (var playerIndex = 0; playerIndex < 8; playerIndex++)
							{
								if (teamIndex == player.team_index && playerIndex == player.player_id)
								{
									line += "-\t";
								}
								else
								{
									line += player.hit_team_player_count[teamIndex, playerIndex] + "\t";
								}
							}
						}
					}
					WriteLine(line);
				}
			}

		}

		private void Clear()
		{
			textBox.Text = string.Empty;
		}

		private void WriteLine(string message = "")
		{
			textBox.Text += message + Environment.NewLine;
			textBox.Select(0, 0);
		}
	}
}
