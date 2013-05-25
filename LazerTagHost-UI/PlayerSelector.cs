
using System;

namespace LazerTagHostUI
{


    [System.ComponentModel.ToolboxItem(true)]
    public partial class PlayerSelector : Gtk.Bin
    {

        private SelectionChanged _listener;
        private readonly Gtk.RadioButton[,] _radioButtonPlayers = new Gtk.RadioButton[3,8];

        public PlayerSelector ()
        {
	        Build();

            Gtk.RadioButton first = null;

            for (uint team_index = 0; team_index < 3; team_index++)
			{
                for (uint player_index = 0; player_index < 8; player_index++)
				{
                    var name = "radiobutton_" + team_index + "_" + player_index;
    
                    var radioButton = new Gtk.RadioButton(first, name);
                    _radioButtonPlayers[team_index,player_index] = radioButton;
    
                    if (first == null) first = radioButton;
    
                    radioButton.CanFocus = true;
                    radioButton.Name = name;
                    radioButton.DrawIndicator = false;
                    radioButton.UseUnderline = true;
                    radioButton.Active = (team_index == 0 && player_index == 0);
                    radioButton.Label = "";
                    radioButton.Clicked += ListenSelectionChanges;
                    //TODO: Store index in rb.data
    
                    tablePlayerSelector.Add(radioButton);
                    Gtk.Table.TableChild tc = ((Gtk.Table.TableChild)(tablePlayerSelector[radioButton]));
                    uint top_offset = 1;
                    tc.TopAttach = player_index + top_offset + 0;
                    tc.BottomAttach = player_index + top_offset + 1;
                    uint left_offset = 0;
                    tc.LeftAttach = team_index + left_offset + 0;
                    tc.RightAttach = team_index + left_offset + 1;
                }
            }

            ShowAll();
        }

        private void ListenSelectionChanges(object sender, EventArgs e)
		{
            if (_listener != null) _listener(GetCurrentSelectedPlayer());
        }

        public void SetListener(SelectionChanged newListener)
		{
            _listener = newListener;
            ListenSelectionChanges(this, new EventArgs());
        }

        public delegate void SelectionChanged(int playerNumber);

        public int GetCurrentSelectedPlayer()
		{
            for (var rowIndex = 0; rowIndex < 3; rowIndex++)
			{
                for (var columnIndex = 0; columnIndex < 8; columnIndex++)
                {
	                var radioButton = _radioButtonPlayers[rowIndex, columnIndex];
                    if (!radioButton.Active) continue;

	                return (rowIndex*8) + columnIndex + 1;
                }
            }
            return 0;
        }

        public void SelectPlayer(uint team_index, uint player_index) {
            if (team_index > 2 || player_index > 7) return;
	        Gtk.RadioButton rb = _radioButtonPlayers[team_index, player_index];
            rb.Active = true;
        }

        public delegate string GetPlayerNameDelegate(int teamNumber, int playerNumber);

        public void RefreshPlayerNames(GetPlayerNameDelegate getPlayerName)
		{
            if (getPlayerName == null) return;

            for (var teamNumber = 1; teamNumber <= 3; teamNumber++) {
                for (var playerNumber = 1; playerNumber <= 8; playerNumber++)
                {
	                var radioButton = _radioButtonPlayers[teamNumber - 1, playerNumber - 1];
                    radioButton.Label = getPlayerName(teamNumber, playerNumber);
                }
            }
        }

        public void SetColumnLabels(string one, string two, string three) {
            labelColumn1.LabelProp = one;
            labelColumn2.LabelProp = two;
            labelColumn3.LabelProp = three;

        }
    }
}
