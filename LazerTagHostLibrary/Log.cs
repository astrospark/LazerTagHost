using System;
using System.ComponentModel;
using System.Diagnostics;

namespace LazerTagHostLibrary
{
	public class Log
	{
		public static void Add(Severity severity, string format, params object[] arguments)
		{
			if (severity < LogLevel) return;
			try
			{
				var message = string.Format(format, arguments);
				var entry = string.Format("{0}\t{1}", DateTime.Now, message);
				Debug.WriteLine(entry);
				OnEntryAdded(severity, entry);
			}
			catch (FormatException ex)
			{
				Add(String.Format("Error in format string: {0}", format), ex);
			}
		}

		public static void Add(string message, Exception ex)
		{
			Add(Severity.Error, string.Join(Environment.NewLine, new[] {message, GetExceptionMessageRecursive(ex)}));
		}

		public static string GetExceptionMessageRecursive(Exception ex)
		{
			return GetExceptionMessageRecursive(ex, Environment.NewLine);
		}

		public static string GetExceptionMessageRecursive(Exception ex, string separator)
		{
			var message = ex.Message;
			if (ex.InnerException != null)
			{
				var innerMessage = GetExceptionMessageRecursive(ex, separator);
				message = string.Join(separator, new[] {message, innerMessage});
			}
			return message;
		}

#if DEBUG
		public static Severity LogLevel = Severity.Debug;
#else
		public static Severity LogLevel = Severity.Information;
#endif

		public delegate void EntryAddedEventHandler(object sender, EntryAddedEventArgs e);

		public static event EntryAddedEventHandler EntryAdded;

		public enum Severity
		{
			Debug = 0,
			Information = 1,
			Warning = 2,
			Error = 3,
		}

		[ImmutableObject(true)]
		public class EntryAddedEventArgs : EventArgs
		{
			public EntryAddedEventArgs(Severity severity, string entry)
			{
				Severity = severity;
				Entry = entry;
			}

			public Severity Severity { get; private set; }
			public string Entry { get; private set; }
		}

		private static void OnEntryAdded(Severity severity, string entry)
		{
			if (EntryAdded != null) EntryAdded(null, new EntryAddedEventArgs(severity, entry));
		}
	}
}
