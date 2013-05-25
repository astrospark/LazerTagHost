
using System;

namespace LazerTagHostUI
{


    [System.ComponentModel.ToolboxItem(true)]
    public partial class PlayerSelectionScreen : Gtk.Bin
    {
        HostControlListener _listener;

        public PlayerSelectionScreen ()
        {
	        Build();

            playerselector.SetListener(SelectionChanged);
        }

        void SelectionChanged(int playerNumber)
		{
			if (_listener != null) _listener.SelectionChanged(playerNumber);
        }

        public PlayerSelector GetPlayerSelector() {
            return playerselector;
        }

        public void SetControlOptions(bool drop,
                                      bool rename,
                                      bool late,
                                      bool abort,
                                      bool next,
                                      bool pause)
        {
            buttonDropPlayer.Sensitive = drop;
            comboboxentryRenamePlayer.Sensitive = rename;
            buttonLateJoin.Sensitive = late;
            buttonAbortHost.Sensitive = abort;
            buttonNext.Sensitive = next;
            buttonPause.Sensitive = pause;
        }

        public void SetTitle(string title) {
            labelTitle.LabelProp = title;
        }

        public interface HostControlListener
        {
            void DropPlayer(object sender, EventArgs e);
            void RenamePlayer(object sender, EventArgs e);
            void LateJoin(object sender, EventArgs e);
            void Abort(object sender, EventArgs e);
            void Pause(object sender, EventArgs e);
            void Next(object sender, EventArgs e);
            void SelectionChanged(int playerNumber);
            void RelativeScoresToggle(bool show_relative);
        }

        public void SubscribeEvents(HostControlListener listener)
        {
            _listener = listener;
            buttonDropPlayer.Clicked += listener.DropPlayer;
            comboboxentryRenamePlayer.Changed += listener.RenamePlayer;
            buttonLateJoin.Clicked += listener.LateJoin;
            buttonAbortHost.Clicked += listener.Abort;
            buttonPause.Clicked += listener.Pause;
            buttonNext.Clicked += listener.Next;

        }

        protected virtual void RelativeScoresToggled (object sender, EventArgs e)
        {
            if (_listener == null) return;

            _listener.RelativeScoresToggle(togglebuttonRelativeScores.Active);
        }
        
        


    }
}
