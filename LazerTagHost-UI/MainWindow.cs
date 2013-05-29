using System;
using System.Linq;
using Gtk;
using LazerTagHostLibrary;
using LazerTagHostUI;

public partial class MainWindow : Window
{
    private readonly HostGun _hostGun;
	private HostWindow _hostWindow = null;
	private readonly GameDefinition _gameDefinition = new GameDefinition();

    public MainWindow () : base(WindowType.Toplevel)
    {
        Build();

        var ports = LazerTagSerial.GetSerialPorts();
		foreach (var port in ports)
		{
			comboboxentryArduinoPorts.AppendText(port);
		}

		_hostGun = new HostGun(null, null);

		if (string.IsNullOrWhiteSpace(LazerTagHostUI.Properties.Settings.Default.SerialPortName))
		{
			foreach (string port in ports.Where(port => _hostGun.SetDevice(port)))
			{
				comboboxentryArduinoPorts.Entry.Text = port;
				buttonStartHost.Sensitive = true;
				SetTranscieverStatusImage("gtk-apply");
				break;
			}
		}
		else
		{
			var model = comboboxentryArduinoPorts.Model;
			TreeIter iter;
			model.GetIterFirst(out iter);
			do
			{
				var value = new GLib.Value();
				model.GetValue(iter, 0, ref value);
				var valueString = value.Val as string;
				if (valueString != null && valueString.Equals(LazerTagHostUI.Properties.Settings.Default.SerialPortName))
				{
					comboboxentryArduinoPorts.SetActiveIter(iter);
					break;
				}
			} while (model.IterNext(ref iter));
		}

        ShowAll();

	    _hostWindow = new HostWindow(_hostGun) {Modal = true};
	    _hostWindow.Hide();

        UpdateGameType();
    }

    private void SetTranscieverStatusImage(string iconName)
    {
        imageTransceiverStatus.Pixbuf = Stetic.IconLoader.LoadIcon(this, iconName, IconSize.Menu);
    }

    protected virtual void TransceiverChanged (object sender, EventArgs e)
    {
	    if (_hostGun == null) return;

	    var serialPortName = comboboxentryArduinoPorts.ActiveText;
		if (_hostGun.SetDevice(serialPortName))
        {
            buttonStartHost.Sensitive = true;
            SetTranscieverStatusImage("gtk-apply");
			LazerTagHostUI.Properties.Settings.Default.SerialPortName = serialPortName;
			LazerTagHostUI.Properties.Settings.Default.Save();
		}
		else
		{
            buttonStartHost.Sensitive = false;
            SetTranscieverStatusImage("gtk-dialog-error");
        }
    }

    protected void OnDeleteEvent (object sender, DeleteEventArgs a)
    {
        Application.Quit ();
        a.RetVal = true;
    }

    private byte ConvertGameValue(int input)
    {
        if (input >= 100 || input < 0) return 0xff;
        return (byte)input;
    }

    protected void StartGameType (object sender, EventArgs e)
    {
	    _gameDefinition.GameTimeMinutes = ConvertGameValue(spinbuttonGameTime.ValueAsInt);
	    _gameDefinition.Tags = ConvertGameValue(spinbuttonTags.ValueAsInt);
	    _gameDefinition.Reloads = ConvertGameValue(spinbuttonReloads.ValueAsInt);
	    _gameDefinition.Shields = ConvertGameValue(spinbuttonShield.ValueAsInt);
	    _gameDefinition.Mega = ConvertGameValue(spinbuttonMega.ValueAsInt);
	    _gameDefinition.TeamTags = checkbuttonFriendlyFire.Active;
	    _gameDefinition.MedicMode = checkbuttonMedicMode.Active;
	    _gameDefinition.CountdownTimeSeconds = spinbuttonCountdownTime.ValueAsInt;

        _hostGun.StartServer(_gameDefinition);

        _hostWindow.Show();
    }

	private void SetGameDefaults(GameType gameType)
	{
		_gameDefinition.GameType = gameType;
		var info = GameTypes.GetInfo(gameType);

		spinbuttonGameTime.Value = info.DefaultGameTimeMinutes;

		spinbuttonGameTime.Adjustment.Upper = 100 - info.GameTimeStepMinutes;
		spinbuttonGameTime.Adjustment.Lower = info.GameTimeStepMinutes;
		spinbuttonGameTime.ClimbRate = info.GameTimeStepMinutes;
		spinbuttonGameTime.Adjustment.StepIncrement = info.GameTimeStepMinutes;

		spinbuttonReloads.Value = info.DefaultReloads;
		spinbuttonMega.Value = info.DefaultMega;
		spinbuttonShield.Value = info.DefaultShields;
		spinbuttonTags.Value = info.DefaultTags;

		checkbuttonFriendlyFire.Sensitive = info.TeamCount > 1;
		checkbuttonFriendlyFire.Active = info.DefaultTeamTags;

		checkbuttonMedicMode.Sensitive = info.TeamCount > 1;
		checkbuttonMedicMode.Active = info.DefaultMedicMode;

		spinbuttonNumberOfTeams.Value = info.TeamCount;
	}

    private void UpdateGameType()
    {
	    switch (comboboxGameType.Active)
	    {
		    case 0: // Custom Laser Tag (Solo)
				SetGameDefaults(GameType.CustomLazerTag);
			    break;
		    case 1: // Own The Zone (Solo)
				SetGameDefaults(GameType.OwnTheZone);
			    break;
			case 2: // Respawn (Solo)
				SetGameDefaults(GameType.Respawn);
				break;
			case 3: // 2-Team Customized Lazer Tag
				SetGameDefaults(GameType.CustomLazerTagTwoTeams);
			    break;
		    case 4: // 3-Team Customized Lazer Tag
				SetGameDefaults(GameType.CustomLazerTagThreeTeams);
			    break;
		    case 5: // Hide And Seek
				SetGameDefaults(GameType.HideAndSeek);
			    break;
		    case 6: // Hunt The Prey
				SetGameDefaults(GameType.HuntThePrey);
			    break;
		    case 7: // 2-Team Kings
				SetGameDefaults(GameType.KingsTwoTeams);
			    break;
			case 8: // 3-Team Kings
				SetGameDefaults(GameType.KingsThreeTeams);
			    break;
		    case 9: // 2-Team Own The Zone
				SetGameDefaults(GameType.OwnTheZoneTwoTeams);
			    break;
		    case 10: // 3-Team Own The Zone
				SetGameDefaults(GameType.OwnTheZoneThreeTeams);
			    break;
	    }
    }

    protected virtual void GameTypeChanged (object sender, EventArgs e)
    {
        UpdateGameType();
    }
}
