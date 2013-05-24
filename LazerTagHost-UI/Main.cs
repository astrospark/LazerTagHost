using System;
using Gtk;
using LazerTagHostLibrary;

namespace LazerTagHostUI
{
    class MainClass
    {
		[STAThread]
        public static void Main (string[] args)
        {
            Application.Init ();
			MainWindow win = new MainWindow();
			//var win = new ScoreReport();
            win.Show ();
            Application.Run ();
        }
    }
}
