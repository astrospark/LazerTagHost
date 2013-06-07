using System;
using System.Windows.Forms;

namespace LazerTagHostUI
{
	public partial class UnhandledException : Form
	{
		public UnhandledException(Exception ex)
		{
			InitializeComponent();
			DisplayException(ex);
		}

		private void DisplayException(Exception ex)
		{
			WriteLine(GetExceptionMessageRecursive(ex));
		}

		private void WriteLine(string format, params object[] arguments)
		{
			textBox.Text += String.Format(format, arguments);
			textBox.Text += Environment.NewLine;
			textBox.Select(textBox.TextLength - 1, 0);
		}

		private static string GetExceptionMessageRecursive(Exception ex, string separator = null)
		{
			if (separator == null) separator = Environment.NewLine;
			var message = ex.Message;
			if (ex.InnerException != null)
			{
				var innerMessage = GetExceptionMessageRecursive(ex.InnerException, separator);
				message = string.Join(separator, new[] {message, innerMessage});
			}
			return message;
		}

		private void buttonClose_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
