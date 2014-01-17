using System;
using System.ComponentModel;
using System.Diagnostics;

namespace LazerTagHostLibrary
{
	public class Log
	{
		#region Public methods

		public static void Add(Severity severity, string format, params object[] arguments)
		{
			try
			{
				var time = DateTime.Now;
				var message = string.Format(format, arguments);
				Debug.WriteLine("{0}\t{1}\t{2}", time, severity, message);
				OnEntryAdded(new Entry(time, severity, message));
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

		#endregion

		#region Events

		private static void OnEntryAdded(Entry entry)
		{
			if (EntryAdded != null) EntryAdded(null, new EntryAddedEventArgs(entry));
		}

		public static event EntryAddedEventHandler EntryAdded;

		public delegate void EntryAddedEventHandler(object sender, EntryAddedEventArgs e);

		[ImmutableObject(true)]
		public class EntryAddedEventArgs : EventArgs
		{
			public EntryAddedEventArgs(Entry entry)
			{
				Entry = entry;
			}

			public Entry Entry { get; private set; }
		}

		#endregion

		#region Public classes

		[ImmutableObject(true)]
		public class Entry
		{
			public Entry(DateTime time, Severity severity, string message)
			{
				Time = time;
				Severity = severity;
				Message = message;
			}

			public DateTime Time { get; private set; }
			public Severity Severity { get; private set; }
			public string Message { get; private set; }
		}

		#endregion

		#region Public enumerations

		public enum Severity
		{
			Debug = 0,
			Information = 1,
			Warning = 2,
			Error = 3,
		}

		#endregion
	}
}
