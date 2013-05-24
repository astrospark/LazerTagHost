
using System;
using System.Collections.Generic;
using LazerTagHostLibrary;

namespace LazerTagHostUI
{


    public partial class HostWindow : Gtk.Window, IHostChangedListener, PlayerSelectionScreen.HostControlListener
    {
        private HostGun hg = null;
        private bool relative_scoresheet = false;

        public HostWindow (HostGun hg) : base(Gtk.WindowType.Toplevel)
        {
            this.Build ();

            GLib.TimeoutHandler th = new GLib.TimeoutHandler(HostUpdate);
            GLib.Timeout.Add(100,th);

            this.hg = hg;

            hg.AddListener(this);

            PlayerSelector ps = playerselectionscreenMain.GetPlayerSelector();

            if (hg.IsTeamGame())
            {
	            ps.SetColumnLabels("Team 1", "Team 2", "Team 3");
            }
			else
            {
	            ps.SetColumnLabels("", "", "");
            }
            playerselectionscreenMain.SubscribeEvents(this);

            RefreshPlayerList();
        }

        private bool HostUpdate()
        {
            hg.Update();

            string title = hg.GetGameStateText() + "\n" + hg.GetCountdown();
            playerselectionscreenMain.SetTitle(title);
    
            return true;
        }

        private Player GetSelectedPlayer()
		{
            var playerSelector = playerselectionscreenMain.GetPlayerSelector();
            if (playerSelector == null) return null;

            var playerNumber = playerSelector.GetCurrentSelectedPlayer();
            if (playerNumber == 0) return null;

            return hg.LookupPlayer(new TeamPlayerId(playerNumber));
        }

        private string GetPlayerName(int teamNumber, int playerNumber)
        {
	        var teamPlayerId = new TeamPlayerId(teamNumber, playerNumber);
			var player = hg.LookupPlayer(teamPlayerId);
			if (player == null) return "Open";

			var text = String.Format("{0} ({1}) ", player.PlayerName, teamPlayerId);
    
            switch (hg.GetGameState())
			{
				case HostGun.HostingState.HOSTING_STATE_SUMMARY:
					text += (player.HasBeenDebriefed() ? "Done" : "Waiting");
					break;
				case HostGun.HostingState.HOSTING_STATE_GAME_OVER:
					text += Ordinal.FromCardinal(player.Rank);

					if (relative_scoresheet)
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
						if (hg.IsZoneGame())
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

            hg.Next();
        }

        void PlayerSelectionScreen.HostControlListener.Pause(object sender, EventArgs e)
        {
            hg.Pause();
        }

        void PlayerSelectionScreen.HostControlListener.Abort (object sender, EventArgs e)
        {
            Console.WriteLine("Abort");
            hg.EndGame();

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

            if (!hg.SetPlayerName(new TeamPlayerId(playerNumber), name)) return;
            RefreshPlayerList();
        }

        void PlayerSelectionScreen.HostControlListener.DropPlayer (object sender, EventArgs e)
        {
            Console.WriteLine("Drop");

			var playerSelector = playerselectionscreenMain.GetPlayerSelector();
			if (playerSelector == null) return;

			var playerNumber = playerSelector.GetCurrentSelectedPlayer();
			if (playerNumber == 0) return;

			hg.DropPlayer(new TeamPlayerId(playerNumber));

            RefreshPlayerList();
        }

        void PlayerSelectionScreen.HostControlListener.SelectionChanged(int playerNumber)
		{
            if (relative_scoresheet) RefreshPlayerList();
        }

        void PlayerSelectionScreen.HostControlListener.RelativeScoresToggle(bool show_relative) {
            relative_scoresheet = show_relative;
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
			if (state == HostGun.HostingState.HOSTING_STATE_GAME_OVER)
			{
				var scoreReportForm = new ScoreReport(hg);
				scoreReportForm.Show();
			}

            //drop,rename,late,abort,next
            switch (state)
			{
				case HostGun.HostingState.HOSTING_STATE_ADDING:
				case HostGun.HostingState.HOSTING_STATE_CONFIRM_JOIN:
					// disable late join
					playerselectionscreenMain.SetControlOptions(true, true, false, true, true, true);
					break;
				case HostGun.HostingState.HOSTING_STATE_PLAYING:
					// disable pause
					playerselectionscreenMain.SetControlOptions(true, true, true, true, true, false);
					break;
				case HostGun.HostingState.HOSTING_STATE_GAME_OVER:
				case HostGun.HostingState.HOSTING_STATE_SUMMARY:
					// disable pause, late join, next, drop
					playerselectionscreenMain.SetControlOptions(false, true, false, true, false, false);
					break;
				case HostGun.HostingState.HOSTING_STATE_COUNTDOWN:
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
