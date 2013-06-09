using System;
using System.Threading;
using Gtk;

namespace LazerTagHostUI
{
    class MainClass
    {
		[STAThread]
        public static void Main (string[] args)
		{
			System.Windows.Forms.Application.ThreadException += Application_ThreadException;
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Application.Init ();
			MainWindow win = new MainWindow();
            win.Show ();
            Application.Run ();
        }

	    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
	    {
			ShowUnhandledExceptionDialog((Exception)e.ExceptionObject);
	    }

	    private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
	    {
		    ShowUnhandledExceptionDialog(e.Exception);
	    }

		private static void ShowUnhandledExceptionDialog(Exception ex)
		{
			var unhandledExceptionForm = new UnhandledException(ex);
			unhandledExceptionForm.ShowDialog();
			Application.Quit();
		}
    }
}
