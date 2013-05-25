
using System;
using System.Collections.Generic;
using LazerTagHostLibrary;

namespace LazerTagHostUI
{


    public partial class HostWindow : Gtk.Window, IHostChangedListener, PlayerSelectionScreen.HostControlListener
    {
        private HostGun _hostGun;
        private bool relativeScoresheet;

        public HostWindow (HostGun hostGun) : base(Gtk.WindowType.Toplevel)
        {
            this.Build ();

            GLib.TimeoutHandler timeoutHandler = HostUpdate;
            GLib.Timeout.Add(100,timeoutHandler);

            _hostGun = hostGun;

            hostGun.AddListener(this);

            var playerSelector = playerselectionscreenMain.GetPlayerSelector();

            if (hostGun.IsTeamGame())
            {
	            playerSelector.SetColumnLabels("Team 1", "Team 2", "Team 3");
            }
			else
            {
	            playerSelector.SetColumnLabels("", "", "");
            }
            playerselectionscreenMain.SubscribeEvents(this);

            RefreshPlayerList();
        }

        private bool HostUpdate()
        {
            _hostGun.Update();

            string title = _hostGun.GetGameStateText() + "\n" + _hostGun.GetCountdown();
            playerselectionscreenMain.SetTitle(title);
    
            return true;
        }

        private Player GetSelectedPlayer()
		{
            var playerSelector = playerselectionscreenMain.GetPlayerSelector();
            if (playerSelector == null) return null;

            var playerNumber = playerSelector.GetCurrentSelectedPlayer();
            if (playerNumber == 0) return null;

            return _hostGun.LookupPlayer(new TeamPlayerId(playerNumber));
        }

        private string GetPlayerName(int teamNumber, int playerNumber)
        {
	        var teamPlayerId = new TeamPlayerId(teamNumber, playerNumber);
			var player = _hostGun.LookupPlayer(teamPlayerId);
			if (player == null) return "Open";

			var text = String.Format("{0} ({1}) ", player.PlayerName, teamPlayerId);
    
            switch (_hostGun.GetGameState())
			{
				case HostGun.HostingState.Summary:
					text += (player.AllTagReportsReceived() ? "Done" : "Waiting");
					break;
				case HostGun.HostingState.GameOver:
					text += Ordinal.FromCardinal(player.Rank);

					if (relativeScoresheet)
					{
						var selectedPlayer = GetSelectedPlayer();
						if (selectedPlayer == player) {
							text += "\nYou";
						} else if (selectedPlayer != null) {

							text += "\nYou Hit: ";
							text += selectedPlayer.TaggedPlayerCounts[teamPlayerId.PlayerNumber - 1];
							text += " Hit You: ";
							text += selectedPlayer.TaggedByPlayerCounts[teamPlayerId.PlayerNumber - 1];
						} else {
							text += "\nUnknown";
						}
					}
					else
					{
						if (_hostGun.IsZoneGame())
						{
							text += string.Format("\nZone Time: {0}", player.ZoneTime.ToString("m:ss"));
						}
						else
						{
							text += String.Format("\nScore: {0} Dmg Recv: {1}", player.Score, player.TagsTaken);
						}
					}

					break;
			}
			return text;
		}
    
        private void RefreshPlayerList()
        {
            var playerSelector = playerselectionscreenMain.GetPlayerSelector();
            if (playerSelector == null) return;
    
            playerSelector.RefreshPlayerNames(GetPlayerName);
        }

#region PlayerSelectionScreen.HostControlListener implmentation

        void PlayerSelectionScreen.HostControlListener.LateJoin (object sender, EventArgs e)
        {
            Console.WriteLine("LateJoin");
        }

        void PlayerSelectionScreen.HostControlListener.Next (object sender, EventArgs e)
        {
            Console.WriteLine("Next");

            _hostGun.Next();
        }

        void PlayerSelectionScreen.HostControlListener.Pause(object sender, EventArgs e)
        {
            _hostGun.Pause();
        }

        void PlayerSelectionScreen.HostControlListener.Abort (object sender, EventArgs e)
        {
            Console.WriteLine("Abort");
            _hostGun.EndGame();

            Hide();
        }

        void PlayerSelectionScreen.HostControlListener.RenamePlayer (object sender, EventArgs e)
        {
            Console.WriteLine("Rename");
            Gtk.ComboBoxEntry entry = (Gtk.ComboBoxEntry)sender;

            if (entry == null) return;

            string name = entry.Entry.Text;

            if (name.Length < 3) return;

            //TODO, append to all, check dups
            entry.AppendText(name);

            var playerSelector = playerselectionscreenMain.GetPlayerSelector();
            if (playerSelector == null) return;

			var playerNumber = playerSelector.GetCurrentSelectedPlayer();
			if (playerNumber == 0) return;

            if (!_hostGun.SetPlayerName(new TeamPlayerId(playerNumber), name)) return;
            RefreshPlayerList();
        }

        void PlayerSelectionScreen.HostControlListener.DropPlayer (object sender, EventArgs e)
        {
            Console.WriteLine("Drop");

			var playerSelector = playerselectionscreenMain.GetPlayerSelector();
			if (playerSelector == null) return;

			var playerNumber = playerSelector.GetCurrentSelectedPlayer();
			if (playerNumber == 0) return;

			_hostGun.DropPlayer(new TeamPlayerId(playerNumber));

            RefreshPlayerList();
        }

        void PlayerSelectionScreen.HostControlListener.SelectionChanged(int playerNumber)
		{
            if (relativeScoresheet) RefreshPlayerList();
        }

        void PlayerSelectionScreen.HostControlListener.RelativeScoresToggle(bool show_relative) {
            relativeScoresheet = show_relative;
            RefreshPlayerList();
        }

#endregion
    
#region HostChangedListener implementation
        void IHostChangedListener.PlayerListChanged(List<Player> players)
		{
            RefreshPlayerList();
        }

        void IHostChangedListener.GameStateChanged(HostGun.HostingState state)
		{
			if (state == HostGun.HostingState.GameOver)
			{
				var scoreReportForm = new ScoreReport(_hostGun);
				scoreReportForm.Show();
			}

            //drop,rename,late,abort,next
            switch (state)
			{
				case HostGun.HostingState.Adding:
				case HostGun.HostingState.ConfirmJoin:
					// disable late join
					playerselectionscreenMain.SetControlOptions(true, true, false, true, true, true);
					break;
				case HostGun.HostingState.Playing:
					// disable pause
					playerselectionscreenMain.SetControlOptions(true, true, true, true, true, false);
					break;
				case HostGun.HostingState.GameOver:
				case HostGun.HostingState.Summary:
					// disable pause, late join, next, drop
					playerselectionscreenMain.SetControlOptions(false, true, false, true, false, false);
					break;
				case HostGun.HostingState.Countdown:
					// disable next, LateJoin, pause
					playerselectionscreenMain.SetControlOptions(true, true, false, true, false, false);
					break;
				default:
					playerselectionscreenMain.SetControlOptions(false, false, false, true, true, false);
					break;
            }
            RefreshPlayerList();
        }
#endregion
    }
}
