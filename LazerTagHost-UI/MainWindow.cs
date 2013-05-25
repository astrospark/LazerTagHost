using System;
using System.Linq;
using Gtk;
using LazerTagHostLibrary;
using LazerTagHostUI;

public partial class MainWindow : Window
{
    private readonly HostGun _hostGun;
    private readonly HostWindow _hostWindow;
    private HostGun.CommandCode _gameType = HostGun.CommandCode.COMMAND_CODE_CUSTOM_GAME_MODE_HOST;

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
        _hostGun.DynamicHostMode(_gameType,
                        ConvertGameValue(spinbuttonGameTime.ValueAsInt),
                        ConvertGameValue(spinbuttonTags.ValueAsInt),
                        ConvertGameValue(spinbuttonReloads.ValueAsInt),
                        ConvertGameValue(spinbuttonShield.ValueAsInt),
                        ConvertGameValue(spinbuttonMega.ValueAsInt),
                        checkbuttonFriendlyFire.Active,
                        checkbuttonMedicMode.Active,
                        ConvertGameValue(spinbuttonNumberOfTeams.ValueAsInt));

        _hostGun.SetGameStartCountdownTime(spinbuttonCountdownTime.ValueAsInt);
        _hostGun.StartServer();
        _hostWindow.Show();
    }

    private void SetGameDefaults(int time, int reloads, int mega, int shields, int tags, bool ff, bool medic, int teams, bool medicEnabled, int timeStep)
    {
        spinbuttonGameTime.Value = time;
        if (timeStep == 2) {
            spinbuttonGameTime.Adjustment.Upper = 98;
            spinbuttonGameTime.Adjustment.Lower = 2;
        } else if (timeStep == 1) {
            spinbuttonGameTime.Adjustment.Upper = 99;
            spinbuttonGameTime.Adjustment.Lower = 1;
        }
        spinbuttonGameTime.ClimbRate = timeStep;
        spinbuttonGameTime.Adjustment.StepIncrement = timeStep;
        spinbuttonReloads.Value = reloads;
        spinbuttonMega.Value = mega;
        spinbuttonShield.Value = shields;
        spinbuttonTags.Value = tags;
        checkbuttonFriendlyFire.Active = ff;
        if (teams <= 1) {
            checkbuttonFriendlyFire.Sensitive = false;
            checkbuttonFriendlyFire.Active = false;
        } else {
            checkbuttonFriendlyFire.Sensitive = true;
        }
        checkbuttonMedicMode.Active = medic;
        if (teams <= 1 || !medicEnabled) {
            checkbuttonMedicMode.Sensitive = false;
            checkbuttonMedicMode.Active = false;
        } else {
            checkbuttonMedicMode.Sensitive = true;
        }
        spinbuttonNumberOfTeams.Value = teams;
    }

    private void UpdateGameType()
    {
        switch (comboboxGameType.Active) {
        case 0:
            //Custom Laser Tag (Solo)
            _gameType = HostGun.CommandCode.COMMAND_CODE_CUSTOM_GAME_MODE_HOST;
            SetGameDefaults(10,100,10,15,10,false,false,1,true,1);
            break;
        case 1:
            //Own The Zone (Solo)
            _gameType = HostGun.CommandCode.COMMAND_CODE_OWN_THE_ZONE_GAME_MODE_HOST;
            SetGameDefaults(10,15,0,45,10,false,false,1,false,1);
            break;
        case 2:
            //2-Team Customized Lazer Tag
            _gameType = HostGun.CommandCode.COMMAND_CODE_2TMS_GAME_MODE_HOST;
            SetGameDefaults(15,100,10,15,20,true,true,2,true,1);
            break;
        case 3:
            //3-Team Customized Lazer Tag
            _gameType = HostGun.CommandCode.COMMAND_CODE_3TMS_GAME_MODE_HOST;
            SetGameDefaults(15,100,10,15,20,true,true,3,true,1);
            break;
        case 4:
            //Hide And Seek
            _gameType = HostGun.CommandCode.COMMAND_CODE_HIDE_AND_SEEK_GAME_MODE_HOST;
            SetGameDefaults(10,5,15,30,25,true,true,2,true,2);
            break;
        case 5:
            //Hunt The Prey
            _gameType = HostGun.CommandCode.COMMAND_CODE_HUNT_THE_PREY_GAME_MODE_HOST;
            SetGameDefaults(10,5,15,30,25,true,true,3,true,1);
            break;
        case 6:
            //2-Team Kings
            _gameType = HostGun.CommandCode.COMMAND_CODE_2_KINGS_GAME_MODE_HOST;
            SetGameDefaults(15,20,0,30,15,true,true,2,true,1);
            break;
        case 7:
            //3-Team Kings
            _gameType = HostGun.CommandCode.COMMAND_CODE_3_KINGS_GAME_MODE_HOST;
            SetGameDefaults(30,20,0,30,15,true,true,3,true,1);
            break;
        case 8:
            //2-Team Own The Zone
            _gameType = HostGun.CommandCode.COMMAND_CODE_2TMS_OWN_THE_ZONE_GAME_MODE_HOST;
            SetGameDefaults(15,15,0,45,10,true,false,2,false,1);
            break;
        case 9:
            //3-Team Own The Zone
            _gameType = HostGun.CommandCode.COMMAND_CODE_3TMS_OWN_THE_ZONE_GAME_MODE_HOST;
            SetGameDefaults(20,15,0,45,10,true,false,3,false,1);
            break;
        }
    }

    protected virtual void GameTypeChanged (object sender, EventArgs e)
    {
        UpdateGameType();
    }
}
